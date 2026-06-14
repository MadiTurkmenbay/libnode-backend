using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class ReadingProgressServiceTests
{
    private readonly IServiceProvider _services;

    public ReadingProgressServiceTests(DatabaseFixture fixture)
    {
        _services = fixture.Services;
    }

    private static async Task<(User user, Book book, Chapter chapter1, Chapter chapter2)> SeedUserBookAndChaptersAsync(AppDbContext context)
    {
        var user = new User
        {
            Username = "progress_user",
            Email = "progress_user@test.local",
            PasswordHash = "hash"
        };
        var book = new Book { Title = "Progress Book" };
        var chapter1 = new Chapter { BookId = book.Id, Title = "Chapter 1", Content = "...", ChapterNumber = 1 };
        var chapter2 = new Chapter { BookId = book.Id, Title = "Chapter 2", Content = "...", ChapterNumber = 2 };
        context.Users.Add(user);
        context.Books.Add(book);
        context.Chapters.Add(chapter1);
        context.Chapters.Add(chapter2);
        await context.SaveChangesAsync();
        return (user, book, chapter1, chapter2);
    }

    [Fact]
    public async Task ConcurrentFirstWrites_DoNotThrow()
    {
        User user;
        Book book;
        Chapter chapter1;
        Chapter chapter2;

        using (var seedScope = _services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            (user, book, chapter1, chapter2) = await SeedUserBookAndChaptersAsync(seedContext);
        }

        var t1 = Task.Run(async () =>
        {
            using var scope = _services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IReadingProgressService>();
            await service.UpsertProgressAsync(user.Id, book.Id, chapter1.Id);
        });

        var t2 = Task.Run(async () =>
        {
            using var scope = _services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IReadingProgressService>();
            await service.UpsertProgressAsync(user.Id, book.Id, chapter2.Id);
        });

        await Task.WhenAll(t1, t2);

        using var assertScope = _services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var progress = await assertContext.ReadingProgresses
            .FirstOrDefaultAsync(rp => rp.UserId == user.Id && rp.BookId == book.Id);

        Assert.NotNull(progress);
        Assert.True(progress.ChapterId == chapter1.Id || progress.ChapterId == chapter2.Id);
    }
}
