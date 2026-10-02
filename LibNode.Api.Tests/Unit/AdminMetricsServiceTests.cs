using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class AdminMetricsServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetMetricsAsync_ReturnsCountsAndRecentSignups()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var users = Enumerable.Range(1, 6)
            .Select(i => new User
            {
                Id = Guid.NewGuid(),
                Username = $"user-{i}",
                Email = $"user-{i}@test.local",
                PasswordHash = "hash",
                AvatarUrl = i == 6 ? "avatars/latest.webp" : null,
                CreatedAt = now.AddMinutes(i),
            })
            .ToList();

        var book = new Book { Id = Guid.NewGuid(), Title = "Metrics book" };
        var team = new Team { Id = Guid.NewGuid(), Name = "Metrics team" };
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            UserId = users[0].Id,
            Content = "Comment",
        };

        context.Users.AddRange(users);
        context.Books.Add(book);
        context.Chapters.Add(new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            Title = "Chapter",
            Content = "Content",
            ChapterNumber = 1,
        });
        context.Comments.Add(comment);
        context.CommentReports.AddRange(
            new CommentReport { Id = Guid.NewGuid(), CommentId = comment.Id, ReporterUserId = users[1].Id, Reason = "Open" },
            new CommentReport { Id = Guid.NewGuid(), CommentId = comment.Id, ReporterUserId = users[2].Id, Reason = "Closed", IsResolved = true });
        context.Teams.Add(team);
        context.TeamTitleRequests.AddRange(
            new TeamTitleRequest { Id = Guid.NewGuid(), TeamId = team.Id, BookId = book.Id, Status = RequestStatus.Pending },
            new TeamTitleRequest { Id = Guid.NewGuid(), TeamId = team.Id, BookId = book.Id, Status = RequestStatus.Rejected });
        context.TeamInvites.AddRange(
            new TeamInvite { Id = Guid.NewGuid(), TeamId = team.Id, UserId = users[0].Id, Status = RequestStatus.Pending },
            new TeamInvite { Id = Guid.NewGuid(), TeamId = team.Id, UserId = users[1].Id, Status = RequestStatus.Approved });
        await context.SaveChangesAsync();

        var service = new AdminMetricsService(context, new FakeStorageService());
        var metrics = await service.GetMetricsAsync();

        Assert.Equal(6, metrics.UserCount);
        Assert.Equal(1, metrics.BookCount);
        Assert.Equal(1, metrics.ChapterCount);
        Assert.Equal(1, metrics.CommentCount);
        Assert.Equal(1, metrics.ReportCount);
        Assert.Equal(1, metrics.TeamCount);
        Assert.Equal(1, metrics.PendingRequests);
        Assert.Equal(1, metrics.PendingInvites);
        Assert.Equal(5, metrics.RecentSignups.Count);
        Assert.Equal("user-6", metrics.RecentSignups[0].Username);
        Assert.Equal("avatars/latest.webp", metrics.RecentSignups[0].AvatarUrl);
        Assert.DoesNotContain(metrics.RecentSignups, u => u.Username == "user-1");
    }
}
