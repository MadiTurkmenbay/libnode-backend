using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class CollectionService : ICollectionService
{
    private readonly AppDbContext _context;

    public CollectionService(AppDbContext context)
    {
        _context = context;
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
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId);

        if (collection == null) return null;

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
                cb.Book.CoverUrl,
                cb.Book.CreatedAt,
                cb.Book.UpdatedAt,
                _context.Chapters.Count(ch => ch.BookId == cb.Book.Id)
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

        var existingLinks = await _context.CollectionBooks
            .Where(cb => cb.BookId == bookId && cb.Collection!.UserId == userId)
            .ToListAsync(ct);

        _context.CollectionBooks.RemoveRange(existingLinks);

        var alreadyInTarget = existingLinks.Any(cb => cb.CollectionId == collectionId);

        if (!alreadyInTarget)
        {
            _context.CollectionBooks.Add(new CollectionBook
            {
                CollectionId = collectionId,
                BookId = bookId
            });
        }

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
