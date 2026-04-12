using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibNode.Api.Services;

/// <summary>
/// Бизнес-логика для работы с главами.
/// </summary>
public class ChapterService : IChapterService
{
    private readonly AppDbContext _db;

    public ChapterService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CursorPagedResult<ChapterListDto, int>> GetByBookIdAsync(Guid bookId, int? cursor, int limit = 50, bool sortDesc = true, Guid? userId = null, CancellationToken ct = default)
    {
        var query = _db.Chapters
            .AsNoTracking()
            .Where(c => c.BookId == bookId);

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
                userId.HasValue && c.Likes.Any(l => l.UserId == userId.Value)
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

    public async Task<ChapterDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken ct = default)
    {
        return await _db.Chapters
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ChapterDetailDto(
                c.Id,
                c.BookId,
                c.Title,
                c.Content,
                c.ChapterNumber,
                c.CreatedAt,
                c.Likes.Count(),
                userId.HasValue && c.Likes.Any(l => l.UserId == userId.Value)
            ))
            .FirstOrDefaultAsync(ct);
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
            ChapterNumber = dto.ChapterNumber
        };

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(ct);

        return new ChapterDetailDto(
            chapter.Id,
            chapter.BookId,
            chapter.Title,
            chapter.Content,
            chapter.ChapterNumber,
            chapter.CreatedAt,
            0,
            false
        );
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
