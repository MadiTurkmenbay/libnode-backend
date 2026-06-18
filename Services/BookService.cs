using System.Globalization;
using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

/// <summary>
/// Реализация бизнес-логики для работы с книгами.
/// </summary>
public class BookService : IBookService
{
    private const char CursorSeparator = '|';
    private const string CursorDateFormat = "o";

    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public BookService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>
    /// Заменяет ключи обложек в DTO на абсолютные публичные URL (в памяти, после материализации).
    /// EF-проекции хранят сырой ключ; склейка URL делается здесь единообразно.
    /// </summary>
    private BookDto ResolveCovers(BookDto b) =>
        b with
        {
            CoverUrl = b.CoverUrl == null ? null : _storage.ResolveUrl(b.CoverUrl),
            CoverThumbUrl = b.CoverThumbUrl == null ? null : _storage.ResolveUrl(b.CoverThumbUrl),
        };

    /// <inheritdoc />
    public async Task<CursorStringPagedResult<BookDto>> GetAllAsync(GetBooksQueryDto query, Guid? userId = null, CancellationToken ct = default)
    {
        var booksQuery = ApplyFilters(_db.Books.AsNoTracking(), query);

        var sortBy = query.SortBy ?? BookSortBy.CreatedAt;
        var descending = IsDescending(query.SortDirection);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var cursor = ParseBookCursor(query.Cursor, sortBy);
            booksQuery = ApplyCursorFilter(booksQuery, sortBy, descending, cursor);
        }

        var orderedQuery = ApplySorting(booksQuery, sortBy, descending);

        var items = await ProjectBooks(orderedQuery.Take(query.Limit + 1), userId, ct);

        var hasMore = items.Count > query.Limit;

        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var nextCursor = hasMore ? EncodeBookCursor(items[^1], sortBy) : null;

        return new CursorStringPagedResult<BookDto>(items, nextCursor, hasMore);
    }

    /// <inheritdoc />
    public async Task<PagedResult<BookDto>> GetAllWithOffsetAsync(GetBooksQueryDto query, Guid? userId = null, CancellationToken ct = default)
    {
        var booksQuery = ApplyFilters(_db.Books.AsNoTracking(), query);

        var totalCount = await booksQuery.CountAsync(ct);

        var sortBy = query.SortBy ?? BookSortBy.CreatedAt;
        var descending = IsDescending(query.SortDirection);
        var orderedQuery = ApplySorting(booksQuery, sortBy, descending);

        var page = Math.Max(1, query.Page);
        var skip = (page - 1) * query.Limit;

        var pagedQuery = orderedQuery
            .Skip(skip)
            .Take(query.Limit);

        var items = await ProjectBooks(pagedQuery, userId, ct);

        return new PagedResult<BookDto>(items, totalCount, page, query.Limit);
    }

    /// <inheritdoc />
    public async Task<BookDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken ct = default)
    {
        var query = _db.Books
            .AsNoTracking()
            .Include(b => b.Tags)
            .Include(b => b.Categories)
            .Where(b => b.Id == id);

        BookDetailDto? result;

        if (userId.HasValue)
        {
            var currentUserId = userId.Value;

            result = await query
                .Select(b => new BookDetailDto(
                    b.Id,
                    b.Title,
                    b.Description,
                    b.CoverUrl,
                    b.CoverThumbUrl,
                    b.Type,
                    b.OriginalStatus,
                    b.TranslationStatus,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.Chapters.Count(c => c.IsPublished),
                    b.ReadingProgresses
                        .Where(rp => rp.UserId == currentUserId)
                        .Select(rp => new ReadingProgressDto(
                            rp.ChapterId,
                            rp.Chapter.ChapterNumber
                        ))
                        .FirstOrDefault(),
                    b.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
                    b.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList()
                ))
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            result = await query
                .Select(b => new BookDetailDto(
                    b.Id,
                    b.Title,
                    b.Description,
                    b.CoverUrl,
                    b.CoverThumbUrl,
                    b.Type,
                    b.OriginalStatus,
                    b.TranslationStatus,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.Chapters.Count(c => c.IsPublished),
                    null,
                    b.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
                    b.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList()
                ))
                .FirstOrDefaultAsync(ct);
        }

        if (result is not null)
        {
            // Атомарный инкремент просмотров тайтла. Не аудируется (UpdatedAt не трогаем,
            // чтобы просмотр не влиял на сортировку каталога) и пока не отдаётся в DTO.
            await _db.Books
                .Where(b => b.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.ViewCount, b => b.ViewCount + 1), ct);

            // Ключи обложек → абсолютные URL (в памяти).
            result = result with
            {
                CoverUrl = result.CoverUrl == null ? null : _storage.ResolveUrl(result.CoverUrl),
                CoverThumbUrl = result.CoverThumbUrl == null ? null : _storage.ResolveUrl(result.CoverThumbUrl),
            };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<BookDto> CreateAsync(CreateBookDto dto, CancellationToken ct = default)
    {
        var book = new Book
        {
            Title = dto.Title,
            Description = dto.Description,
            CoverUrl = dto.CoverUrl,
            Type = dto.Type,
            OriginalStatus = dto.OriginalStatus,
            TranslationStatus = dto.TranslationStatus
        };

        if (dto.TagIds is { Count: > 0 })
        {
            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync(ct);

            foreach (var tag in tags)
            {
                book.Tags.Add(tag);
            }
        }

        if (dto.CategoryIds is { Count: > 0 })
        {
            var categories = await _db.Categories
                .Where(c => dto.CategoryIds.Contains(c.Id))
                .ToListAsync(ct);

            foreach (var category in categories)
            {
                book.Categories.Add(category);
            }
        }

        _db.Books.Add(book);
        await _db.SaveChangesAsync(ct);

        return ResolveCovers(new BookDto(
            book.Id,
            book.Title,
            book.Description,
            book.CoverUrl,
            book.CoverThumbUrl,
            book.Type,
            book.OriginalStatus,
            book.TranslationStatus,
            book.CreatedAt,
            book.UpdatedAt,
            0,
            null,
            book.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
            book.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList()
        ));
    }

    public async Task<BookDto?> UpdateAsync(Guid id, UpdateBookDto dto, CancellationToken ct = default)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book is null) return null;

        book.Title = dto.Title;
        book.Description = dto.Description;
        book.CoverUrl = dto.CoverUrl;
        book.Type = dto.Type;
        book.OriginalStatus = dto.OriginalStatus;
        book.TranslationStatus = dto.TranslationStatus;

        await _db.SaveChangesAsync(ct);

        var chapterCount = await _db.Chapters.CountAsync(c => c.BookId == id && c.IsPublished, ct);
        return ResolveCovers(new BookDto(
            book.Id,
            book.Title,
            book.Description,
            book.CoverUrl,
            book.CoverThumbUrl,
            book.Type,
            book.OriginalStatus,
            book.TranslationStatus,
            book.CreatedAt,
            book.UpdatedAt,
            chapterCount,
            null,
            new List<TagDto>(),
            new List<CategoryDto>()
        ));
    }

    // ── Private helpers ──────────────────────────────────────

    private static bool IsDescending(string? sortDirection) =>
        !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

    private static IQueryable<Book> ApplyFilters(IQueryable<Book> booksQuery, GetBooksQueryDto query)
    {
        var search = query.Search?.Trim();
        var tagSlugs = NormalizeSlugs(query.Tags);
        var categorySlugs = NormalizeSlugs(query.Categories);
        var types = NormalizeEnums(query.Types);
        var originalStatuses = NormalizeEnums(query.OriginalStatuses);
        var translationStatuses = NormalizeEnums(query.TranslationStatuses);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            booksQuery = booksQuery.Where(b =>
                EF.Functions.ILike(b.Title, pattern) ||
                (b.Slug != null && EF.Functions.ILike(b.Slug, pattern)) ||
                (b.Description != null && EF.Functions.ILike(b.Description, pattern)));
        }

        if (types.Length > 0)
        {
            booksQuery = booksQuery.Where(b => types.Contains(b.Type));
        }

        if (originalStatuses.Length > 0)
        {
            booksQuery = booksQuery.Where(b => originalStatuses.Contains(b.OriginalStatus));
        }

        if (translationStatuses.Length > 0)
        {
            booksQuery = booksQuery.Where(b => translationStatuses.Contains(b.TranslationStatus));
        }

        if (tagSlugs.Length > 0)
        {
            booksQuery = booksQuery.Where(b => b.Tags.Any(t => tagSlugs.Contains(t.Slug)));
        }

        if (categorySlugs.Length > 0)
        {
            booksQuery = booksQuery.Where(b => b.Categories.Any(c => categorySlugs.Contains(c.Slug)));
        }

        if (query.TeamId.HasValue)
        {
            booksQuery = booksQuery.Where(b => b.TeamId == query.TeamId.Value);
        }

        return booksQuery;
    }

    private static IOrderedQueryable<Book> ApplySorting(IQueryable<Book> query, BookSortBy sortBy, bool descending)
    {
        return sortBy switch
        {
            BookSortBy.Title => descending
                ? query.OrderByDescending(b => b.Title).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Title).ThenBy(b => b.Id),

            BookSortBy.UpdatedAt => descending
                ? query.OrderByDescending(b => b.UpdatedAt).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.UpdatedAt).ThenBy(b => b.Id),

            // CreatedAt и default
            _ => descending
                ? query.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.CreatedAt).ThenBy(b => b.Id),
        };
    }

    private static IQueryable<Book> ApplyCursorFilter(IQueryable<Book> query, BookSortBy sortBy, bool descending, BookCursor cursor)
    {
        return sortBy switch
        {
            BookSortBy.Title => descending
                ? query.Where(b =>
                    b.Title.CompareTo(cursor.StringValue) < 0 ||
                    (b.Title == cursor.StringValue && b.Id.CompareTo(cursor.Id) < 0))
                : query.Where(b =>
                    b.Title.CompareTo(cursor.StringValue) > 0 ||
                    (b.Title == cursor.StringValue && b.Id.CompareTo(cursor.Id) > 0)),

            BookSortBy.UpdatedAt => descending
                ? query.Where(b =>
                    b.UpdatedAt < cursor.DateValue ||
                    (b.UpdatedAt == cursor.DateValue && b.Id.CompareTo(cursor.Id) < 0))
                : query.Where(b =>
                    b.UpdatedAt > cursor.DateValue ||
                    (b.UpdatedAt == cursor.DateValue && b.Id.CompareTo(cursor.Id) > 0)),

            // CreatedAt и default
            _ => descending
                ? query.Where(b =>
                    b.CreatedAt < cursor.DateValue ||
                    (b.CreatedAt == cursor.DateValue && b.Id.CompareTo(cursor.Id) < 0))
                : query.Where(b =>
                    b.CreatedAt > cursor.DateValue ||
                    (b.CreatedAt == cursor.DateValue && b.Id.CompareTo(cursor.Id) > 0)),
        };
    }

    private static BookCursor ParseBookCursor(string cursor, BookSortBy sortBy)
    {
        var lastSeparatorIndex = cursor.LastIndexOf(CursorSeparator);
        if (lastSeparatorIndex < 0 || lastSeparatorIndex == cursor.Length - 1)
        {
            throw new ArgumentException("Invalid cursor format: missing separator or ID.", nameof(cursor));
        }

        var valuePart = cursor.Substring(0, lastSeparatorIndex);
        var idPart = cursor.Substring(lastSeparatorIndex + 1);

        if (!Guid.TryParse(idPart, out var id))
        {
            throw new ArgumentException("Invalid cursor ID.", nameof(cursor));
        }

        if (sortBy == BookSortBy.Title)
        {
            return new BookCursor(id, stringValue: valuePart);
        }

        if (!DateTime.TryParse(valuePart, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateValue))
        {
            throw new ArgumentException("Invalid cursor date value.", nameof(cursor));
        }

        return new BookCursor(id, dateValue: dateValue);
    }

    private static string EncodeBookCursor(BookDto book, BookSortBy sortBy)
    {
        var value = sortBy switch
        {
            BookSortBy.Title => book.Title,
            BookSortBy.UpdatedAt => book.UpdatedAt.ToString(CursorDateFormat, CultureInfo.InvariantCulture),
            _ => book.CreatedAt.ToString(CursorDateFormat, CultureInfo.InvariantCulture),
        };

        return $"{value}{CursorSeparator}{book.Id}";
    }

    private async Task<List<BookDto>> ProjectBooks(IQueryable<Book> query, Guid? userId, CancellationToken ct)
    {
        List<BookDto> items;
        if (userId.HasValue)
        {
            var currentUserId = userId.Value;

            items = await query
                .Select(b => new BookDto(
                    b.Id,
                    b.Title,
                    b.Description,
                    b.CoverUrl,
                    b.CoverThumbUrl,
                    b.Type,
                    b.OriginalStatus,
                    b.TranslationStatus,
                    b.CreatedAt,
                    b.UpdatedAt,
                    b.Chapters.Count(c => c.IsPublished),
                    b.ReadingProgresses
                        .Where(rp => rp.UserId == currentUserId)
                        .Select(rp => new ReadingProgressDto(
                            rp.ChapterId,
                            rp.Chapter.ChapterNumber
                        ))
                        .FirstOrDefault(),
                    b.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
                    b.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList(),
                    b.Ratings.Any() ? (double?)b.Ratings.Average(r => (double)r.Value) : null,
                    b.Ratings.Count
                ))
                .ToListAsync(ct);

            for (var i = 0; i < items.Count; i++) items[i] = ResolveCovers(items[i]);
            return items;
        }

        items = await query
            .Select(b => new BookDto(
                b.Id,
                b.Title,
                b.Description,
                b.CoverUrl,
                b.CoverThumbUrl,
                b.Type,
                b.OriginalStatus,
                b.TranslationStatus,
                b.CreatedAt,
                b.UpdatedAt,
                b.Chapters.Count(c => c.IsPublished),
                null,
                b.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
                b.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList(),
                b.Ratings.Any() ? (double?)b.Ratings.Average(r => (double)r.Value) : null,
                b.Ratings.Count
            ))
            .ToListAsync(ct);

        for (var i = 0; i < items.Count; i++) items[i] = ResolveCovers(items[i]);
        return items;
    }

    private static TEnum[] NormalizeEnums<TEnum>(IEnumerable<TEnum>? values) where TEnum : struct, Enum
    {
        return values?
            .Distinct()
            .ToArray()
            ?? [];
    }

    private static string[] NormalizeSlugs(IEnumerable<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? [];
    }

    /// <summary>
    /// Разобранный курсор каталога: значение сортировки + ID книги (tie-breaker).
    /// </summary>
    private readonly record struct BookCursor(Guid Id, DateTime? DateValue, string? StringValue)
    {
        public BookCursor(Guid id, DateTime dateValue) : this(id, dateValue, null) { }
        public BookCursor(Guid id, string stringValue) : this(id, null, stringValue) { }
    }
}
