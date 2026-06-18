using System.Linq.Expressions;
using System.Text.RegularExpressions;
using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class CommentService : ICommentService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;
    private readonly ITeamService _teams;
    private readonly INotificationService _notifications;
    private readonly IGamificationService _gamification;

    public CommentService(AppDbContext db, ITeamService teams, INotificationService notifications, IGamificationService gamification)
    {
        _db = db;
        _teams = teams;
        _notifications = notifications;
        _gamification = gamification;
    }

    private static int ClampLimit(int limit) => limit <= 0 ? 20 : Math.Min(limit, MaxLimit);

    private static Expression<Func<Comment, CommentDto>> Projection(Guid? currentUserId) =>
        c => new CommentDto(
            c.Id,
            c.BookId,
            c.ChapterId,
            c.ParentId,
            c.UserId,
            c.User.Username,
            c.Content,
            c.Likes.Sum(l => (int)l.Value),
            currentUserId.HasValue
                ? c.Likes.Where(l => l.UserId == currentUserId.Value).Select(l => (int)l.Value).FirstOrDefault()
                : 0,
            c.IsPinned,
            currentUserId.HasValue && c.UserId == currentUserId.Value,
            c.Replies.Count,
            c.CreatedAt,
            new List<CommentDto>());

    private async Task<List<CommentDto>> WithRepliesAsync(List<CommentDto> roots, Guid? currentUserId, CancellationToken ct)
    {
        if (roots.Count == 0) return roots;

        var rootIds = roots.Select(r => r.Id).ToList();
        var replies = await _db.Comments
            .AsNoTracking()
            .Where(c => c.ParentId != null && rootIds.Contains(c.ParentId.Value))
            .OrderBy(c => c.Id)
            .Select(Projection(currentUserId))
            .ToListAsync(ct);

        var byParent = replies.GroupBy(r => r.ParentId!.Value).ToDictionary(g => g.Key, g => (IReadOnlyList<CommentDto>)g.ToList());

        return roots
            .Select(r => byParent.TryGetValue(r.Id, out var rs) ? r with { Replies = rs } : r)
            .ToList();
    }

    private async Task<CursorPagedResult<CommentDto, Guid>> PageRootsAsync(
        IQueryable<Comment> rootQuery, Guid? currentUserId, Guid? cursor, int limit, bool includePinned, string sort, CancellationToken ct)
    {
        var take = ClampLimit(limit);

        var pinned = new List<CommentDto>();
        if (includePinned && !cursor.HasValue)
        {
            pinned = await rootQuery
                .Where(c => c.IsPinned)
                .OrderByDescending(c => c.Id)
                .Select(Projection(currentUserId))
                .ToListAsync(ct);
        }

        var regularQuery = rootQuery.Where(c => !c.IsPinned);

        // Сортировки top/controversial возвращают одну ограниченную страницу (без курсора).
        if (sort is "top" or "controversial")
        {
            IQueryable<Comment> ordered = sort == "top"
                ? regularQuery.OrderByDescending(c => c.Likes.Sum(l => (int)l.Value)).ThenByDescending(c => c.Id)
                : regularQuery.OrderByDescending(c => c.Likes.Count()).ThenByDescending(c => c.Id);

            var topRows = await ordered.Take(take).Select(Projection(currentUserId)).ToListAsync(ct);
            var topCombined = await WithRepliesAsync([.. pinned, .. topRows], currentUserId, ct);
            return new CursorPagedResult<CommentDto, Guid>(topCombined, null, false);
        }

        // new (по умолчанию): курсор по Id (UUIDv7) desc.
        if (cursor.HasValue)
        {
            regularQuery = regularQuery.Where(c => c.Id.CompareTo(cursor.Value) < 0);
        }

        var rows = await regularQuery
            .OrderByDescending(c => c.Id)
            .Take(take + 1)
            .Select(Projection(currentUserId))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        Guid? nextCursor = hasMore && rows.Count > 0 ? rows[^1].Id : null;
        var combined = await WithRepliesAsync([.. pinned, .. rows], currentUserId, ct);
        return new CursorPagedResult<CommentDto, Guid>(combined, nextCursor, hasMore);
    }

    public Task<CursorPagedResult<CommentDto, Guid>> GetBookCommentsAsync(
        Guid bookId, Guid? currentUserId, Guid? cursor, int limit, string sort, CancellationToken ct = default)
    {
        var query = _db.Comments.AsNoTracking().Where(c => c.BookId == bookId && c.ChapterId == null && c.ParentId == null);
        return PageRootsAsync(query, currentUserId, cursor, limit, includePinned: true, sort, ct);
    }

    public Task<CursorPagedResult<CommentDto, Guid>> GetChapterCommentsAsync(
        Guid chapterId, Guid? currentUserId, Guid? cursor, int limit, string sort, CancellationToken ct = default)
    {
        var query = _db.Comments.AsNoTracking().Where(c => c.ChapterId == chapterId && c.ParentId == null);
        return PageRootsAsync(query, currentUserId, cursor, limit, includePinned: false, sort, ct);
    }

    public async Task<CursorPagedResult<CommentDto, Guid>> GetRecentForModerationAsync(
        Guid? cursor, int limit, CancellationToken ct = default)
    {
        var take = ClampLimit(limit);
        var query = _db.Comments.AsNoTracking();
        if (cursor.HasValue) query = query.Where(c => c.Id.CompareTo(cursor.Value) < 0);

        var rows = await query
            .OrderByDescending(c => c.Id)
            .Take(take + 1)
            .Select(Projection(null))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        Guid? nextCursor = hasMore && rows.Count > 0 ? rows[^1].Id : null;
        return new CursorPagedResult<CommentDto, Guid>(rows, nextCursor, hasMore);
    }

    private async Task CheckNotMutedAsync(Guid userId, CancellationToken ct)
    {
        var isMuted = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId && u.IsMuted, ct);
        if (isMuted) throw new InvalidOperationException("Вы лишены права оставлять комментарии.");
    }

    private async Task ValidateParentAsync(Guid? parentId, Guid bookId, Guid? chapterId, CancellationToken ct)
    {
        if (!parentId.HasValue) return;

        var parent = await _db.Comments
            .AsNoTracking()
            .Where(c => c.Id == parentId.Value)
            .Select(c => new { c.BookId, c.ChapterId, c.ParentId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Родительский комментарий не найден.");

        if (parent.ParentId != null)
            throw new InvalidOperationException("Нельзя отвечать на ответ (поддерживается один уровень вложенности).");

        if (parent.BookId != bookId || parent.ChapterId != chapterId)
            throw new InvalidOperationException("Родительский комментарий относится к другому разделу.");
    }

    private async Task NotifyReplyAsync(Guid parentId, Guid replierId, Guid bookId, CancellationToken ct)
    {
        var parent = await _db.Comments
            .AsNoTracking()
            .Where(c => c.Id == parentId)
            .Select(c => new { c.UserId, AuthorName = c.User.Username })
            .FirstOrDefaultAsync(ct);
        if (parent == null || parent.UserId == replierId) return;

        var replier = await _db.Users.AsNoTracking().Where(u => u.Id == replierId).Select(u => u.Username).FirstOrDefaultAsync(ct);
        await _notifications.CreateAsync(
            parent.UserId,
            NotificationType.CommentReply,
            $"{replier} ответил на ваш комментарий",
            null,
            $"/books/{bookId}",
            ct);
    }

    private async Task NotifyMentionsAsync(string content, Guid authorId, Guid bookId, CancellationToken ct)
    {
        var names = Regex.Matches(content, "@([A-Za-z0-9_]{2,50})")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        if (names.Count == 0) return;

        var users = await _db.Users.AsNoTracking()
            .Where(u => names.Contains(u.Username))
            .Select(u => new { u.Id })
            .ToListAsync(ct);
        if (users.Count == 0) return;

        var author = await _db.Users.AsNoTracking().Where(u => u.Id == authorId).Select(u => u.Username).FirstOrDefaultAsync(ct);
        var targetIds = users.Select(u => u.Id).Where(uid => uid != authorId);
        await _notifications.CreateManyAsync(
            targetIds,
            NotificationType.Mention,
            $"{author} упомянул вас в комментарии",
            null,
            $"/books/{bookId}",
            ct);
    }

    public async Task<CommentDto> CreateBookCommentAsync(Guid bookId, Guid userId, string content, Guid? parentId, CancellationToken ct = default)
    {
        await CheckNotMutedAsync(userId, ct);
        var bookExists = await _db.Books.AnyAsync(b => b.Id == bookId, ct);
        if (!bookExists) throw new KeyNotFoundException($"Книга с Id {bookId} не найдена.");

        await ValidateParentAsync(parentId, bookId, null, ct);

        var comment = new Comment { BookId = bookId, ChapterId = null, ParentId = parentId, UserId = userId, Content = content.Trim() };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        if (parentId.HasValue) await NotifyReplyAsync(parentId.Value, userId, bookId, ct);
        await NotifyMentionsAsync(comment.Content, userId, bookId, ct);
        await _gamification.AwardCommentAsync(userId, ct);
        return await LoadDtoAsync(comment.Id, userId, ct);
    }

    public async Task<CommentDto> CreateChapterCommentAsync(Guid chapterId, Guid userId, string content, Guid? parentId, CancellationToken ct = default)
    {
        await CheckNotMutedAsync(userId, ct);
        var chapter = await _db.Chapters
            .AsNoTracking()
            .Select(c => new { c.Id, c.BookId })
            .FirstOrDefaultAsync(c => c.Id == chapterId, ct)
            ?? throw new KeyNotFoundException($"Глава с Id {chapterId} не найдена.");

        await ValidateParentAsync(parentId, chapter.BookId, chapterId, ct);

        var comment = new Comment { BookId = chapter.BookId, ChapterId = chapterId, ParentId = parentId, UserId = userId, Content = content.Trim() };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);

        if (parentId.HasValue) await NotifyReplyAsync(parentId.Value, userId, chapter.BookId, ct);
        await NotifyMentionsAsync(comment.Content, userId, chapter.BookId, ct);
        await _gamification.AwardCommentAsync(userId, ct);
        return await LoadDtoAsync(comment.Id, userId, ct);
    }

    private async Task<CommentDto> LoadDtoAsync(Guid commentId, Guid currentUserId, CancellationToken ct)
    {
        return await _db.Comments.AsNoTracking().Where(c => c.Id == commentId).Select(Projection(currentUserId)).FirstAsync(ct);
    }

    public async Task<CommentVoteResultDto> VoteAsync(Guid commentId, Guid userId, short value, CancellationToken ct = default)
    {
        var exists = await _db.Comments.AnyAsync(c => c.Id == commentId, ct);
        if (!exists) throw new KeyNotFoundException($"Комментарий с Id {commentId} не найден.");

        var existing = await _db.CommentLikes.FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId, ct);

        if (value == 0)
        {
            if (existing != null) _db.CommentLikes.Remove(existing);
        }
        else
        {
            var normalized = (short)(value > 0 ? 1 : -1);
            if (existing == null) _db.CommentLikes.Add(new CommentLike { CommentId = commentId, UserId = userId, Value = normalized });
            else existing.Value = normalized;
        }

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { /* concurrent vote */ }

        var score = await _db.CommentLikes.Where(l => l.CommentId == commentId).SumAsync(l => (int)l.Value, ct);
        var myVote = await _db.CommentLikes.Where(l => l.CommentId == commentId && l.UserId == userId).Select(l => (int)l.Value).FirstOrDefaultAsync(ct);
        return new CommentVoteResultDto(commentId, score, myVote);
    }

    public async Task<bool> SetPinnedAsync(Guid commentId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment == null) return false;

        var canManage = await _teams.CanManageBookTitleAsync(userId, comment.BookId, isAdmin, ct);
        if (!canManage) throw new UnauthorizedAccessException("Недостаточно прав для закрепления.");

        if (comment.ChapterId != null || comment.ParentId != null)
            throw new InvalidOperationException("Закреплять можно только корневые комментарии тайтла.");

        var newState = !comment.IsPinned;
        if (newState)
        {
            await _db.Comments
                .Where(c => c.BookId == comment.BookId && c.ChapterId == null && c.IsPinned && c.Id != commentId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsPinned, false), ct);
        }

        comment.IsPinned = newState;
        await _db.SaveChangesAsync(ct);
        return newState;
    }

    public async Task<bool> DeleteAsync(Guid commentId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment == null) return false;

        if (comment.UserId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Можно удалять только свои комментарии.");

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── Reports ──────────────────────────────────────────

    public async Task ReportAsync(Guid commentId, Guid reporterUserId, string reason, CancellationToken ct = default)
    {
        var exists = await _db.Comments.AnyAsync(c => c.Id == commentId, ct);
        if (!exists) throw new KeyNotFoundException("Комментарий не найден.");

        var dup = await _db.CommentReports.AnyAsync(r => r.CommentId == commentId && r.ReporterUserId == reporterUserId && !r.IsResolved, ct);
        if (dup) throw new InvalidOperationException("Вы уже пожаловались на этот комментарий.");

        _db.CommentReports.Add(new CommentReport { CommentId = commentId, ReporterUserId = reporterUserId, Reason = reason.Trim() });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CursorPagedResult<CommentReportDto, Guid>> GetReportsAsync(bool? resolved, Guid? cursor, int limit, CancellationToken ct = default)
    {
        var take = ClampLimit(limit);
        var query = _db.CommentReports.AsNoTracking();
        if (resolved.HasValue) query = query.Where(r => r.IsResolved == resolved.Value);
        if (cursor.HasValue) query = query.Where(r => r.Id.CompareTo(cursor.Value) < 0);

        var rows = await query
            .OrderByDescending(r => r.Id)
            .Take(take + 1)
            .Select(r => new CommentReportDto(
                r.Id,
                r.CommentId,
                r.Comment.BookId,
                r.Comment.ChapterId,
                r.Comment.Content,
                r.Comment.User.Username,
                r.Reporter.Username,
                r.Reason,
                r.IsResolved,
                r.CreatedAt))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        Guid? nextCursor = hasMore && rows.Count > 0 ? rows[^1].Id : null;
        return new CursorPagedResult<CommentReportDto, Guid>(rows, nextCursor, hasMore);
    }

    public async Task<bool> ResolveReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _db.CommentReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report == null) return false;
        report.IsResolved = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
