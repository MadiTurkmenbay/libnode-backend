using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class LeaderboardService : ILeaderboardService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public LeaderboardService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    private string? Avatar(string? key) => key == null ? null : _storage.ResolveUrl(key);

    public async Task<LeaderboardDto> GetAsync(int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, MaxLimit);

        // Топ по XP.
        var topXpRaw = await _db.UserStats.AsNoTracking()
            .Where(s => s.Xp > 0)
            .OrderByDescending(s => s.Xp)
            .Take(take)
            .Select(s => new { s.UserId, s.User.Username, s.User.AvatarUrl, s.Level, Value = s.Xp })
            .ToListAsync(ct);

        // Топ по самой длинной серии.
        var topStreakRaw = await _db.UserStats.AsNoTracking()
            .Where(s => s.LongestStreak > 0)
            .OrderByDescending(s => s.LongestStreak)
            .Take(take)
            .Select(s => new { s.UserId, s.User.Username, s.User.AvatarUrl, s.Level, Value = s.LongestStreak })
            .ToListAsync(ct);

        // Топ комментаторов по суммарному рейтингу комментариев (сумма голосов под их комментами).
        var topCommentersRaw = await _db.CommentLikes.AsNoTracking()
            .GroupBy(l => l.Comment.UserId)
            .Select(g => new { UserId = g.Key, Value = g.Sum(x => (int)x.Value) })
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .Take(take)
            .ToListAsync(ct);

        // Подтягиваем публичные поля авторов для таблицы комментаторов.
        var commenterIds = topCommentersRaw.Select(x => x.UserId).ToList();
        var commenterMeta = await _db.Users.AsNoTracking()
            .Where(u => commenterIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Username, u.AvatarUrl })
            .ToDictionaryAsync(u => u.Id, ct);
        var levels = await _db.UserStats.AsNoTracking()
            .Where(s => commenterIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.Level })
            .ToDictionaryAsync(s => s.UserId, s => s.Level, ct);

        var topXp = topXpRaw
            .Select((s, i) => new LeaderboardEntryDto(i + 1, s.UserId, s.Username, Avatar(s.AvatarUrl), s.Level, s.Value))
            .ToList();
        var topStreak = topStreakRaw
            .Select((s, i) => new LeaderboardEntryDto(i + 1, s.UserId, s.Username, Avatar(s.AvatarUrl), s.Level, s.Value))
            .ToList();
        var topCommenters = topCommentersRaw
            .Where(x => commenterMeta.ContainsKey(x.UserId))
            .Select((x, i) => new LeaderboardEntryDto(
                i + 1,
                x.UserId,
                commenterMeta[x.UserId].Username,
                Avatar(commenterMeta[x.UserId].AvatarUrl),
                levels.TryGetValue(x.UserId, out var lvl) ? lvl : 1,
                x.Value))
            .ToList();

        return new LeaderboardDto(topXp, topStreak, topCommenters);
    }
}
