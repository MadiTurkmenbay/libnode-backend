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
    public async Task<PagedResult<BookDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Books.AsNoTracking();
        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(b => MapToDto(b))
            .ToListAsync(ct);

        return new PagedResult<BookDto>(items, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<BookDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => MapToDto(b))
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
            Id: book.Id,
            Title: book.Title,
            Description: book.Description,
            CoverUrl: book.CoverUrl,
            CreatedAt: book.CreatedAt,
            UpdatedAt: book.UpdatedAt,
            ChapterCount: 0
        );
    }

    // ── Mapping ─────────────────────────────────────────
    /// <summary>
    /// Проекция сущности Book в DTO.
    /// Используется как Expression для server-side evaluation в EF.
    /// </summary>
    private static BookDto MapToDto(Book b) => new(
        Id: b.Id,
        Title: b.Title,
        Description: b.Description,
        CoverUrl: b.CoverUrl,
        CreatedAt: b.CreatedAt,
        UpdatedAt: b.UpdatedAt,
        ChapterCount: b.Chapters.Count
    );
}
