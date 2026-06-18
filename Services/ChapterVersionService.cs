using System.Linq.Expressions;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class ChapterVersionService : IChapterVersionService
{
    private readonly AppDbContext _db;
    private readonly ITeamService _teams;
    private readonly INotificationService _notifications;

    public ChapterVersionService(AppDbContext db, ITeamService teams, INotificationService notifications)
    {
        _db = db;
        _teams = teams;
        _notifications = notifications;
    }

    private static Expression<Func<ChapterVersion, ChapterVersionDto>> ListProjection(Guid? currentUserId) =>
        v => new ChapterVersionDto(
            v.Id,
            v.ChapterId,
            v.TeamId,
            v.Team != null ? v.Team.Name : null,
            v.Title,
            v.Language,
            v.Votes.Sum(x => (int)x.Value),
            currentUserId.HasValue
                ? v.Votes.Where(x => x.UserId == currentUserId.Value).Select(x => (int)x.Value).FirstOrDefault()
                : 0,
            v.IsPublished,
            currentUserId.HasValue && v.CreatedByUserId == currentUserId.Value,
            v.CreatedAt);

    public async Task<IReadOnlyList<ChapterVersionDto>> GetVersionsAsync(Guid chapterId, Guid? currentUserId, CancellationToken ct = default)
    {
        return await _db.ChapterVersions
            .AsNoTracking()
            .Where(v => v.ChapterId == chapterId && v.IsPublished)
            .OrderByDescending(v => v.Votes.Sum(x => (int)x.Value))
            .ThenBy(v => v.CreatedAt)
            .Select(ListProjection(currentUserId))
            .ToListAsync(ct);
    }

    public async Task<ChapterVersionDetailDto?> GetVersionDetailAsync(Guid versionId, Guid? currentUserId, CancellationToken ct = default)
    {
        return await _db.ChapterVersions
            .AsNoTracking()
            .Where(v => v.Id == versionId)
            .Select(v => new ChapterVersionDetailDto(
                v.Id,
                v.ChapterId,
                v.TeamId,
                v.Team != null ? v.Team.Name : null,
                v.Title,
                v.Content,
                v.Language,
                v.Votes.Sum(x => (int)x.Value),
                currentUserId.HasValue
                    ? v.Votes.Where(x => x.UserId == currentUserId.Value).Select(x => (int)x.Value).FirstOrDefault()
                    : 0,
                v.IsPublished,
                v.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ChapterVersionDto> CreateAsync(Guid chapterId, Guid userId, CreateChapterVersionDto dto, CancellationToken ct = default)
    {
        var chapter = await _db.Chapters.AsNoTracking()
            .Where(c => c.Id == chapterId)
            .Select(c => new { c.Id, c.BookId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Глава не найдена.");

        // Команда автора версии = команда тайтла, если пользователь в ней состоит.
        var teamId = (await _teams.GetBookTeamAsync(chapter.BookId, ct))?.TeamId;

        var version = new ChapterVersion
        {
            ChapterId = chapterId,
            TeamId = teamId,
            CreatedByUserId = userId,
            Title = dto.Title.Trim(),
            Content = dto.Content,
            Language = string.IsNullOrWhiteSpace(dto.Language) ? "ru" : dto.Language.Trim(),
            IsPublished = true,
        };
        _db.ChapterVersions.Add(version);
        await _db.SaveChangesAsync(ct);

        // Уведомляем подписчиков (книга в коллекции), кроме автора версии; dedupe по UserId.
        if (version.IsPublished)
            await NotifyVersionPublishedAsync(chapter.BookId, chapterId, version.Title, userId, ct);

        return await _db.ChapterVersions.AsNoTracking()
            .Where(v => v.Id == version.Id)
            .Select(ListProjection(userId))
            .FirstAsync(ct);
    }

    /// <summary>Уведомление подписчиков книги о новой/опубликованной ветке перевода (с dedupe и исключением автора).</summary>
    private async Task NotifyVersionPublishedAsync(Guid bookId, Guid chapterId, string title, Guid authorId, CancellationToken ct)
    {
        var bookmarkerIds = await _db.CollectionBooks
            .Where(cb => cb.BookId == bookId && cb.Collection.UserId != authorId)
            .Select(cb => cb.Collection.UserId)
            .Distinct()
            .ToListAsync(ct);
        if (bookmarkerIds.Count == 0) return;
        await _notifications.CreateManyAsync(
            bookmarkerIds,
            NotificationType.NewChapter,
            $"Новая версия перевода: {title}",
            null,
            $"/books/{bookId}/read/{chapterId}",
            ct);
    }

    public async Task<ChapterVersionDto?> UpdateAsync(Guid versionId, Guid userId, bool isAdmin, UpdateChapterVersionDto dto, CancellationToken ct = default)
    {
        var version = await _db.ChapterVersions.FirstOrDefaultAsync(v => v.Id == versionId, ct);
        if (version == null) return null;

        var canEdit = isAdmin
            || version.CreatedByUserId == userId
            || (version.TeamId != null && await _db.TeamMembers.AnyAsync(m => m.TeamId == version.TeamId && m.UserId == userId, ct));
        if (!canEdit)
            throw new UnauthorizedAccessException("Редактировать можно только версии своей команды.");

        var wasPublished = version.IsPublished;
        version.Title = dto.Title.Trim();
        version.Content = dto.Content;
        if (!string.IsNullOrWhiteSpace(dto.Language)) version.Language = dto.Language.Trim();
        version.IsPublished = dto.IsPublished;
        version.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Переход из черновика в опубликованную ветку — уведомляем подписчиков.
        if (!wasPublished && version.IsPublished)
        {
            var bookId = await _db.Chapters.AsNoTracking().Where(c => c.Id == version.ChapterId).Select(c => c.BookId).FirstAsync(ct);
            await NotifyVersionPublishedAsync(bookId, version.ChapterId, version.Title, userId, ct);
        }

        return await _db.ChapterVersions.AsNoTracking()
            .Where(v => v.Id == version.Id)
            .Select(ListProjection(userId))
            .FirstAsync(ct);
    }

    public async Task<ChapterVersionVoteResultDto> VoteAsync(Guid versionId, Guid userId, short value, CancellationToken ct = default)
    {
        var exists = await _db.ChapterVersions.AnyAsync(v => v.Id == versionId, ct);
        if (!exists) throw new KeyNotFoundException("Версия не найдена.");

        var existing = await _db.ChapterVersionVotes.FirstOrDefaultAsync(x => x.ChapterVersionId == versionId && x.UserId == userId, ct);
        if (value == 0)
        {
            if (existing != null) _db.ChapterVersionVotes.Remove(existing);
        }
        else
        {
            var normalized = (short)(value > 0 ? 1 : -1);
            if (existing == null) _db.ChapterVersionVotes.Add(new ChapterVersionVote { ChapterVersionId = versionId, UserId = userId, Value = normalized });
            else existing.Value = normalized;
        }

        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { /* concurrent vote */ }

        var score = await _db.ChapterVersionVotes.Where(x => x.ChapterVersionId == versionId).SumAsync(x => (int)x.Value, ct);
        var myVote = await _db.ChapterVersionVotes.Where(x => x.ChapterVersionId == versionId && x.UserId == userId).Select(x => (int)x.Value).FirstOrDefaultAsync(ct);
        return new ChapterVersionVoteResultDto(versionId, score, myVote);
    }

    public async Task<bool> DeleteAsync(Guid versionId, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var version = await _db.ChapterVersions.FirstOrDefaultAsync(v => v.Id == versionId, ct);
        if (version == null) return false;
        if (version.CreatedByUserId != userId && !isAdmin) throw new UnauthorizedAccessException("Можно удалять только свои версии.");
        _db.ChapterVersions.Remove(version);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Guid?> GetBookIdForVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        return await _db.ChapterVersions.AsNoTracking()
            .Where(v => v.Id == versionId)
            .Select(v => (Guid?)v.Chapter.BookId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid?> GetBookIdForChapterAsync(Guid chapterId, CancellationToken ct = default)
    {
        return await _db.Chapters.AsNoTracking()
            .Where(c => c.Id == chapterId)
            .Select(c => (Guid?)c.BookId)
            .FirstOrDefaultAsync(ct);
    }
}
