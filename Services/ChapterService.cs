using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibNode.Api.Services;

/// <summary>
/// Бизнес-логика для работы с главами.
/// </summary>
public class ChapterService : IChapterService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public ChapterService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<CursorPagedResult<ChapterListDto, int>> GetByBookIdAsync(Guid bookId, int? cursor, int limit = 50, bool sortDesc = true, Guid? userId = null, bool includeUnpublished = false, CancellationToken ct = default)
    {
        var query = _db.Chapters
            .AsNoTracking()
            .Where(c => c.BookId == bookId);

        // Черновики видны только команде/админу.
        if (!includeUnpublished)
        {
            query = query.Where(c => c.IsPublished);
        }

        // Курсорная фильтрация по ChapterNumber
        if (cursor.HasValue)
        {
            query = sortDesc
                ? query.Where(c => c.ChapterNumber < cursor.Value)
                : query.Where(c => c.ChapterNumber > cursor.Value);
        }

        // Сортировка и лимит (limit + 1 для определения HasMore)
        var orderedQuery = sortDesc
            ? query.OrderByDescending(c => c.ChapterNumber)
            : query.OrderBy(c => c.ChapterNumber);

        var items = await orderedQuery
            .Take(limit + 1)
            .Select(c => new ChapterListDto(
                c.Id,
                c.BookId,
                c.Title,
                c.ChapterNumber,
                c.CreatedAt,
                c.Likes.Count(),
                userId.HasValue && c.Likes.Any(l => l.UserId == userId.Value),
                c.IsPublished
            ))
            .ToListAsync(ct);

        var hasMore = items.Count > limit;

        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = hasMore ? items[^1].ChapterNumber : (int?)null;

        return new CursorPagedResult<ChapterListDto, int>(items, nextCursor, hasMore);
    }

    public async Task<ChapterDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, bool includeUnpublished = false, CancellationToken ct = default)
    {
        var chapter = await _db.Chapters
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.BookId,
                c.Title,
                c.Content,
                c.ChapterNumber,
                c.CreatedAt,
                c.IsPublished,
                LikesCount = c.Likes.Count(),
                IsLikedByCurrentUser = userId.HasValue && c.Likes.Any(l => l.UserId == userId.Value)
            })
            .FirstOrDefaultAsync(ct);

        if (chapter is null)
            return null;

        // Черновик доступен только команде/админу.
        if (!chapter.IsPublished && !includeUnpublished)
            return null;

        var neighborQuery = _db.Chapters.AsNoTracking().Where(c => c.BookId == chapter.BookId);
        if (!includeUnpublished)
        {
            neighborQuery = neighborQuery.Where(c => c.IsPublished);
        }

        var previousId = await neighborQuery
            .Where(c => c.ChapterNumber < chapter.ChapterNumber)
            .OrderByDescending(c => c.ChapterNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        var nextId = await neighborQuery
            .Where(c => c.ChapterNumber > chapter.ChapterNumber)
            .OrderBy(c => c.ChapterNumber)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        return new ChapterDetailDto(
            chapter.Id,
            chapter.BookId,
            chapter.Title,
            chapter.Content,
            chapter.ChapterNumber,
            chapter.CreatedAt,
            chapter.LikesCount,
            chapter.IsLikedByCurrentUser,
            previousId == Guid.Empty ? null : previousId,
            nextId == Guid.Empty ? null : nextId,
            chapter.IsPublished
        );
    }

    public async Task<ChapterDetailDto> CreateAsync(CreateChapterDto dto, CancellationToken ct = default)
    {
        // Проверяем существование книги
        var bookExists = await _db.Books.AnyAsync(b => b.Id == dto.BookId, ct);
        if (!bookExists)
            throw new ArgumentException($"Книга с Id {dto.BookId} не найдена.");

        var chapter = new Chapter
        {
            BookId = dto.BookId,
            Title = dto.Title,
            Content = dto.Content,
            ChapterNumber = dto.ChapterNumber,
            IsPublished = dto.IsPublished
        };

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(ct);

        // Уведомляем о новой главе только при публикации (черновики не шлём).
        if (chapter.IsPublished)
        {
            await NotifyNewChapterAsync(dto.BookId, chapter.Id, chapter.Title, ct);
        }

        return new ChapterDetailDto(
            chapter.Id,
            chapter.BookId,
            chapter.Title,
            chapter.Content,
            chapter.ChapterNumber,
            chapter.CreatedAt,
            0,
            false,
            null,
            null,
            chapter.IsPublished
        );
    }

    private async Task NotifyNewChapterAsync(Guid bookId, Guid chapterId, string title, CancellationToken ct)
    {
        var bookmarkerIds = await _db.CollectionBooks
            .Where(cb => cb.BookId == bookId)
            .Select(cb => cb.Collection.UserId)
            .Distinct()
            .ToListAsync(ct);
        await _notifications.CreateManyAsync(
            bookmarkerIds,
            NotificationType.NewChapter,
            $"Новая глава: {title}",
            null,
            $"/books/{bookId}/read/{chapterId}",
            ct);
    }

    public async Task<Guid?> GetBookIdAsync(Guid chapterId, CancellationToken ct = default)
    {
        var bookId = await _db.Chapters
            .AsNoTracking()
            .Where(c => c.Id == chapterId)
            .Select(c => (Guid?)c.BookId)
            .FirstOrDefaultAsync(ct);
        return bookId;
    }

    public async Task<ChapterDetailDto?> UpdateAsync(Guid id, UpdateChapterDto dto, CancellationToken ct = default)
    {
        var chapter = await _db.Chapters.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (chapter is null) return null;

        var wasPublished = chapter.IsPublished;

        chapter.Title = dto.Title;
        chapter.Content = dto.Content;
        chapter.ChapterNumber = dto.ChapterNumber;
        chapter.IsPublished = dto.IsPublished;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Глава с таким номером уже существует в книге.");
        }

        // Публикация черновика → уведомляем подписчиков (закладки).
        if (!wasPublished && chapter.IsPublished)
        {
            await NotifyNewChapterAsync(chapter.BookId, chapter.Id, chapter.Title, ct);
        }

        return await GetByIdAsync(id, null, includeUnpublished: true, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var chapter = await _db.Chapters.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (chapter is null) return false;
        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task LikeChapterAsync(Guid chapterId, Guid userId, CancellationToken ct = default)
    {
        // Проверяем, есть ли такая глава
        var chapterExists = await _db.Chapters.AnyAsync(c => c.Id == chapterId, ct);
        if (!chapterExists)
            throw new ArgumentException($"Глава с Id {chapterId} не найдена.");

        var like = new ChapterLike
        {
            ChapterId = chapterId,
            UserId = userId
        };

        try
        {
            _db.ChapterLikes.Add(like);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Идемпотентность: юзер уже поставил лайк.
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
