using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class CollectionService : ICollectionService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storage;

    public CollectionService(AppDbContext context, IStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto)
    {
        var collection = new UserCollection
        {
            UserId = userId,
            Name = dto.Name
        };

        _context.UserCollections.Add(collection);
        await _context.SaveChangesAsync();

        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            CreatedAt = collection.CreatedAt,
            BookCount = 0
        };
    }

    public async Task<CollectionDto?> RenameCollectionAsync(Guid collectionId, Guid userId, CreateCollectionDto dto, CancellationToken ct = default)
    {
        var collection = await _context.UserCollections.FirstOrDefaultAsync(c => c.Id == collectionId, ct);
        if (collection == null) return null;
        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("Collection access denied.");

        collection.Name = dto.Name.Trim();
        await _context.SaveChangesAsync(ct);
        var bookCount = await _context.CollectionBooks.CountAsync(cb => cb.CollectionId == collectionId, ct);
        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            CreatedAt = collection.CreatedAt,
            BookCount = bookCount
        };
    }

    public async Task<bool> DeleteCollectionAsync(Guid collectionId, Guid userId, CancellationToken ct = default)
    {
        var collection = await _context.UserCollections.FirstOrDefaultAsync(c => c.Id == collectionId, ct);
        if (collection == null) return false;
        if (collection.UserId != userId)
            throw new UnauthorizedAccessException("Collection access denied.");

        // Existing EF/DB cascade removes this collection's links, not the books.
        _context.UserCollections.Remove(collection);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<CollectionDto>> GetUserCollectionsAsync(Guid userId)
    {
        var collections = await _context.UserCollections
            .Where(c => c.UserId == userId)
            .Select(c => new CollectionDto
            {
                Id = c.Id,
                Name = c.Name,
                CreatedAt = c.CreatedAt,
                BookCount = c.CollectionBooks.Count
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return collections;
    }

    public async Task<CollectionDetailDto?> GetCollectionByIdAsync(Guid collectionId, Guid userId)
    {
        var collection = await _context.UserCollections
            .Include(c => c.CollectionBooks)
                .ThenInclude(cb => cb.Book)
                    .ThenInclude(b => b!.Tags)
            .Include(c => c.CollectionBooks)
                .ThenInclude(cb => cb.Book)
                    .ThenInclude(b => b!.Categories)
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId);

        if (collection == null) return null;

        var bookIds = collection.CollectionBooks.Select(cb => cb.BookId).ToList();
        var chapterCounts = await _context.Chapters
            .AsNoTracking()
            .Where(ch => bookIds.Contains(ch.BookId) && ch.IsPublished)
            .GroupBy(ch => ch.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);
        var ratingStats = await _context.BookRatings
            .AsNoTracking()
            .Where(r => bookIds.Contains(r.BookId))
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count(), Average = g.Average(r => (double)r.Value) })
            .ToDictionaryAsync(x => x.BookId, x => new { x.Count, x.Average });

        return new CollectionDetailDto
        {
            Id = collection.Id,
            Name = collection.Name,
            CreatedAt = collection.CreatedAt,
            BookCount = collection.CollectionBooks.Count,
            Books = collection.CollectionBooks.Select(cb => new BookDto(
                cb.Book!.Id,
                cb.Book.Title,
                cb.Book.Description,
                cb.Book.CoverUrl == null ? null : _storage.ResolveUrl(cb.Book.CoverUrl),
                cb.Book.CoverThumbUrl == null ? null : _storage.ResolveUrl(cb.Book.CoverThumbUrl),
                cb.Book.Type,
                cb.Book.OriginalStatus,
                cb.Book.TranslationStatus,
                cb.Book.CreatedAt,
                cb.Book.UpdatedAt,
                chapterCounts.GetValueOrDefault(cb.Book.Id),
                null,
                cb.Book.Tags.Select(t => new TagDto(t.Id, t.Name, t.Slug)).ToList(),
                cb.Book.Categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug)).ToList(),
                ratingStats.TryGetValue(cb.Book.Id, out var stats) ? stats.Average : null,
                stats?.Count ?? 0
            )).ToList()
        };
    }

    public async Task<IEnumerable<Guid>> GetCollectionIdsWithBookAsync(Guid bookId, Guid userId)
    {
        return await _context.CollectionBooks
            .Where(cb => cb.BookId == bookId && cb.Collection!.UserId == userId)
            .Select(cb => cb.CollectionId)
            .ToListAsync();
    }

    public async Task AddBookToCollectionAsync(Guid collectionId, Guid bookId, Guid userId, CancellationToken ct = default)
    {
        var collection = await _context.UserCollections
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId, ct);

        if (collection == null)
            throw new UnauthorizedAccessException("Collection not found or access denied.");

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var existingLink = await _context.CollectionBooks
            .FirstOrDefaultAsync(cb => cb.BookId == bookId && cb.Collection!.UserId == userId, ct);

        if (existingLink != null && existingLink.CollectionId == collectionId)
        {
            await transaction.CommitAsync(ct);
            return;
        }

        if (existingLink != null)
            _context.CollectionBooks.Remove(existingLink);

        _context.CollectionBooks.Add(new CollectionBook
        {
            CollectionId = collectionId,
            BookId = bookId
        });

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task RemoveBookFromCollectionAsync(Guid collectionId, Guid bookId, Guid userId)
    {
        var collection = await _context.UserCollections
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId);

        if (collection == null)
            throw new UnauthorizedAccessException("Collection not found or access denied.");

        var cb = await _context.CollectionBooks
            .FirstOrDefaultAsync(x => x.CollectionId == collectionId && x.BookId == bookId);

        if (cb != null)
        {
            _context.CollectionBooks.Remove(cb);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<BookCollectionStatusDto?> GetBookCollectionStatusAsync(Guid bookId, Guid userId)
    {
        var link = await _context.CollectionBooks
            .Where(cb => cb.BookId == bookId && cb.Collection!.UserId == userId)
            .Select(cb => new BookCollectionStatusDto
            {
                CollectionId = cb.CollectionId,
                CollectionName = cb.Collection!.Name
            })
            .FirstOrDefaultAsync();

        return link;
    }
}
