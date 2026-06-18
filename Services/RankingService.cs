using System.Linq.Expressions;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class RankingService : IRankingService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;

    public RankingService(AppDbContext db)
    {
        _db = db;
    }

    private static readonly Expression<Func<Book, BookDto>> Projection = b => new BookDto(
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
        b.Ratings.Count);

    public async Task<IReadOnlyList<BookDto>> GetRankingAsync(RankingType type, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, MaxLimit);
        var query = _db.Books.AsNoTracking();

        IOrderedQueryable<Book> ordered = type switch
        {
            RankingType.TopRated => query
                .Where(b => b.Ratings.Any())
                .OrderByDescending(b => b.Ratings.Average(r => (double)r.Value))
                .ThenByDescending(b => b.Ratings.Count),
            RankingType.MostChapters => query
                .OrderByDescending(b => b.Chapters.Count(c => c.IsPublished))
                .ThenByDescending(b => b.Id),
            RankingType.Newest => query
                .OrderByDescending(b => b.CreatedAt)
                .ThenByDescending(b => b.Id),
            _ => query // Popular
                .OrderByDescending(b => b.ViewCount)
                .ThenByDescending(b => b.Id),
        };

        return await ordered.Take(take).Select(Projection).ToListAsync(ct);
    }
}
