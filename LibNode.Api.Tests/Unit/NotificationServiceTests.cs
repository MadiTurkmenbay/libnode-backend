using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class NotificationServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetPrefsAsync_WhenPrefsMissing_ReturnsAllEnabledDefaults()
    {
        await using var context = CreateContext();
        var service = new NotificationService(context);

        var prefs = await service.GetPrefsAsync(Guid.NewGuid());

        Assert.True(prefs.EnableCommentReply);
        Assert.True(prefs.EnableTeamInvite);
        Assert.True(prefs.EnableRequestApproved);
        Assert.True(prefs.EnableRequestRejected);
        Assert.True(prefs.EnableNewChapter);
        Assert.True(prefs.EnableMention);
        Assert.True(prefs.EnableLevelUp);
        Assert.True(prefs.EnableAchievement);
    }

    [Fact]
    public async Task UpdatePrefsAsync_AppliesPartialUpdateAndPreservesUnspecifiedFlags()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        context.UserNotificationPrefs.Add(new UserNotificationPrefs
        {
            UserId = userId,
            EnableMention = false,
            EnableAchievement = false,
        });
        await context.SaveChangesAsync();

        var service = new NotificationService(context);
        var prefs = await service.UpdatePrefsAsync(userId, new UpdateNotificationPrefsDto(
            EnableNewChapter: false,
            EnableLevelUp: false));

        Assert.False(prefs.EnableMention);
        Assert.False(prefs.EnableAchievement);
        Assert.False(prefs.EnableNewChapter);
        Assert.False(prefs.EnableLevelUp);
        Assert.True(prefs.EnableCommentReply);
        Assert.True(prefs.EnableTeamInvite);
    }
}
