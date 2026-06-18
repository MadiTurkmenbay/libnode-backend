using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class GamificationService : IGamificationService
{
    private const int XpPerChapter = 10;
    private const int XpPerComment = 5;

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAchievementService _achievements;
    private readonly IQuestService _quests;

    public GamificationService(AppDbContext db, INotificationService notifications, IAchievementService achievements, IQuestService quests)
    {
        _db = db;
        _notifications = notifications;
        _achievements = achievements;
        _quests = quests;
    }

    private async Task<UserStats> GetOrCreateAsync(Guid userId, CancellationToken ct)
    {
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (stats == null)
        {
            stats = new UserStats { UserId = userId, Level = 1 };
            _db.UserStats.Add(stats);
        }
        return stats;
    }

    public async Task AwardChapterReadAsync(Guid userId, CancellationToken ct = default)
    {
        var stats = await GetOrCreateAsync(userId, ct);
        stats.ChaptersRead++;

        // Серия: тот же день — без изменений; вчера — продолжаем; иначе — сброс на 1.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (stats.LastReadDate != today)
        {
            stats.CurrentStreak = stats.LastReadDate == today.AddDays(-1) ? stats.CurrentStreak + 1 : 1;
            stats.LastReadDate = today;
            if (stats.CurrentStreak > stats.LongestStreak) stats.LongestStreak = stats.CurrentStreak;
        }

        // Дневные квесты на чтение → бонус XP за выполнение засчитываем вместе с базовым.
        var questBonus = await _quests.RecordChapterReadAsync(userId, ct);
        await ApplyXpAsync(stats, userId, XpPerChapter + questBonus, ct);

        // Достижения: ночное чтение (00:00–06:00 UTC) + кумулятивные пороги.
        var nightRead = DateTime.UtcNow.Hour < 6;
        await _achievements.EvaluateAsync(userId, nightRead, ct);
    }

    public async Task AwardCommentAsync(Guid userId, CancellationToken ct = default)
    {
        var stats = await GetOrCreateAsync(userId, ct);
        stats.CommentsPosted++;
        var questBonus = await _quests.RecordCommentAsync(userId, ct);
        await ApplyXpAsync(stats, userId, XpPerComment + questBonus, ct);
        await _achievements.EvaluateAsync(userId, false, ct);
    }

    private async Task ApplyXpAsync(UserStats stats, Guid userId, int amount, CancellationToken ct)
    {
        var oldLevel = stats.Level;
        stats.Xp += amount;
        var newLevel = IGamificationService.LevelForXp(stats.Xp);
        stats.Level = newLevel;
        await _db.SaveChangesAsync(ct);

        if (newLevel > oldLevel)
        {
            await _notifications.CreateAsync(
                userId,
                NotificationType.LevelUp,
                $"Новый уровень: {newLevel}!",
                "Продолжайте читать и комментировать, чтобы расти дальше.",
                "/profile",
                ct);
        }
    }

    public async Task<UserStatsDto> GetStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var stats = await _db.UserStats.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        var xp = stats?.Xp ?? 0;
        var level = stats?.Level ?? IGamificationService.LevelForXp(xp);

        var floor = IGamificationService.XpToReachLevel(level);
        var ceil = IGamificationService.XpToReachLevel(level + 1);
        return new UserStatsDto(
            xp,
            level,
            xp - floor,
            Math.Max(1, ceil - floor),
            stats?.ChaptersRead ?? 0,
            stats?.CommentsPosted ?? 0,
            stats?.CurrentStreak ?? 0,
            stats?.LongestStreak ?? 0);
    }
}
