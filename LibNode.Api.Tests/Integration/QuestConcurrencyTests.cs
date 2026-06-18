using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

/// <summary>
/// Гонки read-then-insert по дневным квестам и статистике (CONCERNS 4.1):
/// конкурентные первые записи не должны падать на 23505 и должны начислять XP ровно один раз.
/// </summary>
[Collection("DatabaseCollection")]
public class QuestConcurrencyTests
{
    private readonly IServiceProvider _services;

    public QuestConcurrencyTests(DatabaseFixture fixture)
    {
        _services = fixture.Services;
    }

    private async Task<Guid> SeedUserAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"quest_user_{Guid.NewGuid():N}",
            Email = $"quest_user_{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task QuestService_ConcurrentFirstRecord_DoesNotThrow()
    {
        var userId = await SeedUserAsync();

        async Task Record()
        {
            using var scope = _services.CreateScope();
            var quests = scope.ServiceProvider.GetRequiredService<IQuestService>();
            await quests.RecordChapterReadAsync(userId);
        }

        await Task.WhenAll(Record(), Record());

        // Прогресс read-1 должен учитывать оба чтения (read-1 target=1 → выдан, read-3 target=3).
        using var assertScope = _services.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await db.UserQuests
            .Where(q => q.UserId == userId && q.Day == day)
            .ToListAsync();

        // Ровно одна строка на (UserId, QuestKey, Day) — нет дублей.
        Assert.Equal(rows.Select(r => r.QuestKey).Distinct().Count(), rows.Count);
        Assert.Contains(rows, r => r.QuestKey == "read-1" && r.Rewarded);
    }

    [Fact]
    public async Task GamificationService_ConcurrentFirstAward_CreatesSingleStatsRow()
    {
        var userId = await SeedUserAsync();

        async Task Award()
        {
            using var scope = _services.CreateScope();
            var gamification = scope.ServiceProvider.GetRequiredService<IGamificationService>();
            await gamification.AwardCommentAsync(userId);
        }

        await Task.WhenAll(Award(), Award());

        using var assertScope = _services.CreateScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var statsCount = await db.UserStats.CountAsync(s => s.UserId == userId);
        Assert.Equal(1, statsCount);

        var xp = await db.UserStats.Where(s => s.UserId == userId).Select(s => s.Xp).FirstAsync();
        Assert.True(xp > 0);
    }
}
