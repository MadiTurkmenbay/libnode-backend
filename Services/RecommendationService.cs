using System.Linq.Expressions;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class RecommendationService : IRecommendationService
{
    private const int MaxLimit = 24;
    private readonly AppDbContext _db;

    public RecommendationService(AppDbContext db)
    {
        _db = db;
    }

    // Проекция книги в BookDto (с рейтингом). UserProgress не заполняем для рекомендаций.
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

    public async Task<IReadOnlyList<BookDto>> GetSimilarAsync(Guid bookId, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 8 : Math.Min(limit, MaxLimit);

        var tagIds = await _db.Books.Where(b => b.Id == bookId).SelectMany(b => b.Tags.Select(t => t.Id)).ToListAsync(ct);
        var catIds = await _db.Books.Where(b => b.Id == bookId).SelectMany(b => b.Categories.Select(c => c.Id)).ToListAsync(ct);

        if (tagIds.Count == 0 && catIds.Count == 0)
            return [];

        return await _db.Books.AsNoTracking()
            .Where(b => b.Id != bookId
                && (b.Tags.Any(t => tagIds.Contains(t.Id)) || b.Categories.Any(c => catIds.Contains(c.Id))))
            .OrderByDescending(b => b.Tags.Count(t => tagIds.Contains(t.Id)) + b.Categories.Count(c => catIds.Contains(c.Id)))
            .ThenByDescending(b => b.ViewCount)
            .Take(take)
            .Select(Projection)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BookDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 12 : Math.Min(limit, MaxLimit);

        // Книги, которые пользователь уже читал.
        var readBookIds = await _db.ChapterReads.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => r.BookId)
            .Distinct()
            .ToListAsync(ct);

        if (readBookIds.Count == 0)
        {
            // Фолбэк: популярное по просмотрам.
            return await _db.Books.AsNoTracking()
                .OrderByDescending(b => b.ViewCount)
                .ThenByDescending(b => b.Id)
                .Take(take)
                .Select(Projection)
                .ToListAsync(ct);
        }

        var tagIds = await _db.Books.Where(b => readBookIds.Contains(b.Id)).SelectMany(b => b.Tags.Select(t => t.Id)).Distinct().ToListAsync(ct);
        var catIds = await _db.Books.Where(b => readBookIds.Contains(b.Id)).SelectMany(b => b.Categories.Select(c => c.Id)).Distinct().ToListAsync(ct);

        var query = _db.Books.AsNoTracking().Where(b => !readBookIds.Contains(b.Id));

        if (tagIds.Count > 0 || catIds.Count > 0)
        {
            query = query
                .Where(b => b.Tags.Any(t => tagIds.Contains(t.Id)) || b.Categories.Any(c => catIds.Contains(c.Id)))
                .OrderByDescending(b => b.Tags.Count(t => tagIds.Contains(t.Id)) + b.Categories.Count(c => catIds.Contains(c.Id)))
                .ThenByDescending(b => b.ViewCount);
        }
        else
        {
            query = query.OrderByDescending(b => b.ViewCount).ThenByDescending(b => b.Id);
        }

        return await query.Take(take).Select(Projection).ToListAsync(ct);
    }
}
