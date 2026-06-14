using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class ReadingProgressServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(User user, Book book, Chapter chapter1, Chapter chapter2)> SeedUserBookAndChaptersAsync(AppDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "progress_user",
            Email = "progress_user@test.local",
            PasswordHash = "hash"
        };
        var book = new Book { Id = Guid.NewGuid(), Title = "Progress Book" };
        var chapter1 = new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            Title = "Chapter 1",
            Content = "...",
            ChapterNumber = 1
        };
        var chapter2 = new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            Title = "Chapter 2",
            Content = "...",
            ChapterNumber = 2
        };
        context.Users.Add(user);
        context.Books.Add(book);
        context.Chapters.Add(chapter1);
        context.Chapters.Add(chapter2);
        await context.SaveChangesAsync();
        return (user, book, chapter1, chapter2);
    }

    [Fact]
    public async Task UpsertProgressAsync_RaceFirstWrites_DoesNotThrow()
    {
        using var context = CreateContext();
        var (user, book, chapter1, chapter2) = await SeedUserBookAndChaptersAsync(context);
        var service = new ReadingProgressService(context);

        var exception = await Record.ExceptionAsync(async () =>
        {
            await service.UpsertProgressAsync(user.Id, book.Id, chapter1.Id);
            await service.UpsertProgressAsync(user.Id, book.Id, chapter2.Id);
        });

        Assert.Null(exception);
        var progress = await context.ReadingProgresses.FindAsync(user.Id, book.Id);
        Assert.NotNull(progress);
        Assert.True(progress.ChapterId == chapter1.Id || progress.ChapterId == chapter2.Id);
    }
}
