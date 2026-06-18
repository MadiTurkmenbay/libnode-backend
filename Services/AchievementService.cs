using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class AchievementService : IAchievementService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public AchievementService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <summary>Снимок состояния пользователя для проверки условий достижений.</summary>
    private readonly record struct Snapshot(
        int Chapters, int Comments, int LongestStreak, int Bookmarks, bool HasGoldComment, bool NightRead);

    /// <summary>Определение достижения: метаданные + условие по снимку.</summary>
    private sealed record Def(string Key, string Title, string Description, string Icon, Func<Snapshot, bool> Unlocked);

    private static readonly Def[] Catalog =
    [
        new("first-chapter", "Первая глава", "Прочитайте первую главу", "BookOpen", s => s.Chapters >= 1),
        new("chapters-10", "Книжный червь", "10 прочитанных глав", "BookMarked", s => s.Chapters >= 10),
        new("chapters-100", "Библиофил", "100 прочитанных глав", "Library", s => s.Chapters >= 100),
        new("streak-7", "Неделя подряд", "Серия чтения 7 дней", "Flame", s => s.LongestStreak >= 7),
        new("streak-30", "Железная воля", "Серия чтения 30 дней", "Flame", s => s.LongestStreak >= 30),
        new("first-comment", "Слово за слово", "Оставьте первый комментарий", "MessageSquare", s => s.Comments >= 1),
        new("commentator", "Комментатор", "50 комментариев", "MessagesSquare", s => s.Comments >= 50),
        new("night-owl", "Ночная сова", "Читайте ночью (00:00–06:00)", "Moon", s => s.NightRead),
        new("collector-10", "Коллекционер", "10 книг в закладках", "Bookmark", s => s.Bookmarks >= 10),
        new("beloved-gold", "Любимец публики", "Комментарий достиг золотого ранга (100+)", "Award", s => s.HasGoldComment),
    ];

    private async Task<Snapshot> BuildSnapshotAsync(Guid userId, bool nightRead, CancellationToken ct)
    {
        var chapters = await _db.ChapterReads.CountAsync(r => r.UserId == userId, ct);
        var comments = await _db.Comments.CountAsync(c => c.UserId == userId, ct);
        var longestStreak = await _db.UserStats.Where(s => s.UserId == userId).Select(s => s.LongestStreak).FirstOrDefaultAsync(ct);
        var bookmarks = await _db.CollectionBooks
            .Where(cb => cb.Collection.UserId == userId)
            .Select(cb => cb.BookId).Distinct().CountAsync(ct);
        var hasGold = await _db.Comments
            .Where(c => c.UserId == userId)
            .AnyAsync(c => c.Likes.Sum(l => (int)l.Value) >= 100, ct);
        return new Snapshot(chapters, comments, longestStreak, bookmarks, hasGold, nightRead);
    }

    public async Task EvaluateAsync(Guid userId, bool nightRead = false, CancellationToken ct = default)
    {
        var snapshot = await BuildSnapshotAsync(userId, nightRead, ct);
        var owned = await _db.UserAchievements
            .Where(a => a.UserId == userId)
            .Select(a => a.Key)
            .ToListAsync(ct);
        var ownedSet = owned.ToHashSet();

        var newlyUnlocked = new List<Def>();
        foreach (var def in Catalog)
        {
            if (ownedSet.Contains(def.Key)) continue;
            if (!def.Unlocked(snapshot)) continue;
            _db.UserAchievements.Add(new UserAchievement { UserId = userId, Key = def.Key, UnlockedAt = DateTime.UtcNow });
            newlyUnlocked.Add(def);
        }

        if (newlyUnlocked.Count == 0) return;
        await _db.SaveChangesAsync(ct);

        foreach (var def in newlyUnlocked)
        {
            await _notifications.CreateAsync(
                userId,
                NotificationType.Achievement,
                $"Достижение получено: {def.Title}",
                def.Description,
                "/profile",
                ct);
        }
    }

    public async Task<IReadOnlyList<AchievementDto>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Ленивый пересчёт кумулятивных достижений (главы/комменты/закладки/золото).
        await EvaluateAsync(userId, nightRead: false, ct);

        var unlocked = await _db.UserAchievements
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.Key, a => a.UnlockedAt, ct);

        return Catalog
            .Select(d => new AchievementDto(
                d.Key, d.Title, d.Description, d.Icon,
                unlocked.ContainsKey(d.Key),
                unlocked.TryGetValue(d.Key, out var at) ? at : null))
            .ToList();
    }
}
