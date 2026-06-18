using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class RatingService : IRatingService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;

    public RatingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RatingAggregateDto> UpsertAsync(Guid userId, Guid bookId, short value, string? review, CancellationToken ct = default)
    {
        if (value is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(value));
        if (!await _db.Books.AnyAsync(b => b.Id == bookId, ct))
            throw new KeyNotFoundException("Книга не найдена.");

        var trimmed = string.IsNullOrWhiteSpace(review) ? null : review.Trim();
        var existing = await _db.BookRatings.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == bookId, ct);
        if (existing == null)
        {
            _db.BookRatings.Add(new BookRating { UserId = userId, BookId = bookId, Value = value, Review = trimmed });
        }
        else
        {
            existing.Value = value;
            existing.Review = trimmed;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);

        return await GetAggregateAsync(bookId, userId, ct);
    }

    public async Task<RatingAggregateDto> GetAggregateAsync(Guid bookId, Guid? currentUserId, CancellationToken ct = default)
    {
        var grouped = await _db.BookRatings.AsNoTracking()
            .Where(r => r.BookId == bookId)
            .GroupBy(r => r.Value)
            .Select(g => new { Star = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var distribution = new int[5];
        var total = 0;
        long sum = 0;
        foreach (var g in grouped)
        {
            if (g.Star is >= 1 and <= 5) distribution[g.Star - 1] = g.Count;
            total += g.Count;
            sum += (long)g.Star * g.Count;
        }
        double? average = total > 0 ? Math.Round((double)sum / total, 2) : null;

        short? myValue = null;
        string? myReview = null;
        if (currentUserId.HasValue)
        {
            var mine = await _db.BookRatings.AsNoTracking()
                .Where(r => r.BookId == bookId && r.UserId == currentUserId.Value)
                .Select(r => new { r.Value, r.Review })
                .FirstOrDefaultAsync(ct);
            if (mine != null) { myValue = mine.Value; myReview = mine.Review; }
        }

        return new RatingAggregateDto(average, total, distribution, myValue, myReview);
    }

    public async Task<CursorPagedResult<ReviewDto, DateTime>> ListReviewsAsync(Guid bookId, DateTime? cursor, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, MaxLimit);

        var query = _db.BookRatings.AsNoTracking()
            .Where(r => r.BookId == bookId && r.Review != null);
        if (cursor.HasValue) query = query.Where(r => r.UpdatedAt < cursor.Value);

        var rows = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Take(take + 1)
            .Select(r => new ReviewDto(
                r.UserId,
                r.User.Username,
                r.User.AvatarThumbUrl,
                r.Value,
                r.Review,
                r.UpdatedAt))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        DateTime? next = hasMore && rows.Count > 0 ? rows[^1].UpdatedAt : null;
        return new CursorPagedResult<ReviewDto, DateTime>(rows, next, hasMore);
    }
}
