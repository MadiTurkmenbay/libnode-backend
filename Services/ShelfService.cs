using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class ShelfService : IShelfService
{
    private readonly AppDbContext _db;

    public ShelfService(AppDbContext db)
    {
        _db = db;
    }

    public async Task UpsertAsync(Guid userId, Guid bookId, ShelfStatus status, CancellationToken ct = default)
    {
        if (!await _db.Books.AnyAsync(b => b.Id == bookId, ct))
            throw new KeyNotFoundException("Книга не найдена.");

        var existing = await _db.BookShelves.FirstOrDefaultAsync(s => s.UserId == userId && s.BookId == bookId, ct);
        if (existing == null)
        {
            _db.BookShelves.Add(new BookShelf { UserId = userId, BookId = bookId, Status = status });
        }
        else
        {
            existing.Status = status;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid bookId, CancellationToken ct = default)
    {
        var existing = await _db.BookShelves.FirstOrDefaultAsync(s => s.UserId == userId && s.BookId == bookId, ct);
        if (existing == null) return false;
        _db.BookShelves.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShelfStatus?> GetStatusAsync(Guid userId, Guid bookId, CancellationToken ct = default)
    {
        var row = await _db.BookShelves.AsNoTracking()
            .Where(s => s.UserId == userId && s.BookId == bookId)
            .Select(s => (ShelfStatus?)s.Status)
            .FirstOrDefaultAsync(ct);
        return row;
    }

    public async Task<IReadOnlyList<ShelfItemDto>> GetShelvesAsync(Guid userId, ShelfStatus? status, CancellationToken ct = default)
    {
        var query = _db.BookShelves.AsNoTracking().Where(s => s.UserId == userId);
        if (status.HasValue) query = query.Where(s => s.Status == status.Value);

        return await query
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new ShelfItemDto(
                s.Status,
                s.UpdatedAt,
                new BookDto(
                    s.Book.Id,
                    s.Book.Title,
                    s.Book.Description,
                    s.Book.CoverUrl,
                    s.Book.CoverThumbUrl,
                    s.Book.Type,
                    s.Book.OriginalStatus,
                    s.Book.TranslationStatus,
                    s.Book.CreatedAt,
                    s.Book.UpdatedAt,
                    s.Book.Chapters.Count(c => c.IsPublished),
                    null,
                    s.Book.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
                    s.Book.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList())))
            .ToListAsync(ct);
    }
}
