using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

/// <summary>
/// Реализация бизнес-логики для работы с книгами.
/// </summary>
public class BookService : IBookService
{
    private readonly AppDbContext _db;

    public BookService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<CursorPagedResult<BookDto, Guid>> GetAllAsync(Guid? cursor, int limit = 20, Guid? userId = null, CancellationToken ct = default)
    {
        var query = _db.Books.AsNoTracking();

        if (cursor.HasValue)
        {
            query = query.Where(b => b.Id < cursor.Value);
        }

        var orderedQuery = query
            .OrderByDescending(b => b.Id)
            .Take(limit + 1);

        List<BookDto> items;

        if (userId.HasValue)
        {
            var currentUserId = userId.Value;

            items = await orderedQuery
                .Select(b => new BookDto(
                    b.Id,
                    b.Title,
                    b.Description,
                    b.CoverUrl,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.Chapters.Count,
                    b.ReadingProgresses
                        .Where(rp => rp.UserId == currentUserId)
                        .Select(rp => new ReadingProgressDto(
                            rp.ChapterId,
                            rp.Chapter.ChapterNumber
                        ))
                        .FirstOrDefault()
                ))
                .ToListAsync(ct);
        }
        else
        {
            items = await orderedQuery
                .Select(b => new BookDto(
                    b.Id,
                    b.Title,
                    b.Description,
                    b.CoverUrl,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.Chapters.Count,
                    null
                ))
                .ToListAsync(ct);
        }

        var hasMore = items.Count > limit;

        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = hasMore ? items[^1].Id : (Guid?)null;

        return new CursorPagedResult<BookDto, Guid>(items, nextCursor, hasMore);
    }

    /// <inheritdoc />
    public async Task<BookDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken ct = default)
    {
        var query = _db.Books
            .AsNoTracking()
            .Where(b => b.Id == id);

        if (userId.HasValue)
        {
            var currentUserId = userId.Value;

            return await query
                .Select(b => new BookDetailDto(
                    b.Id,
                    b.Title,
                    b.Description,
                    b.CoverUrl,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.Chapters.Count,
                    b.ReadingProgresses
                        .Where(rp => rp.UserId == currentUserId)
                        .Select(rp => new ReadingProgressDto(
                            rp.ChapterId,
                            rp.Chapter.ChapterNumber
                        ))
                        .FirstOrDefault()
                ))
                .FirstOrDefaultAsync(ct);
        }

        return await query
            .Select(b => new BookDetailDto(
                b.Id,
                b.Title,
                b.Description,
                b.CoverUrl,
                b.CreatedAt,
                b.UpdatedAt,
                b.Chapters.Count,
                null
            ))
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<BookDto> CreateAsync(CreateBookDto dto, CancellationToken ct = default)
    {
        var book = new Book
        {
            Title = dto.Title,
            Description = dto.Description,
            CoverUrl = dto.CoverUrl
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync(ct);

        return new BookDto(
            book.Id,
            book.Title,
            book.Description,
            book.CoverUrl,
            book.CreatedAt,
            book.UpdatedAt,
            0,
            null
        );
    }
}
