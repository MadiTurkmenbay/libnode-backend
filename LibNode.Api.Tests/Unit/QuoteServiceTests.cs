using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class QuoteServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static User CreateUser(string suffix)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = $"user_{suffix}",
            Email = $"user_{suffix}@test.local",
            PasswordHash = "hash"
        };
    }

    private static Book CreateBook(string suffix)
    {
        return new Book
        {
            Id = Guid.NewGuid(),
            Title = $"Book {suffix}"
        };
    }

    private static Chapter CreateChapter(Book book, string suffix, int number)
    {
        return new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            Title = $"Chapter {suffix}",
            Content = "Chapter content",
            ChapterNumber = number
        };
    }

    [Fact]
    public async Task CreateQuoteAsync_WithValidData_ReturnsQuoteDto()
    {
        using var context = CreateContext();
        var user = CreateUser("create");
        var book = CreateBook("create");
        var chapter = CreateChapter(book, "create", 1);
        context.Users.Add(user);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        var dto = new CreateQuoteDto(chapter.Id, "Selected text", "Context text", "My note");
        var quote = await service.CreateQuoteAsync(user.Id, dto);

        Assert.NotEqual(Guid.Empty, quote.Id);
        Assert.Equal(chapter.Id, quote.ChapterId);
        Assert.Equal(book.Id, quote.BookId);
        Assert.Equal("Selected text", quote.SelectedText);
        Assert.Equal("Context text", quote.ContextText);
        Assert.Equal("My note", quote.Note);
        Assert.Equal(book.Title, quote.BookTitle);
        Assert.Equal(chapter.Title, quote.ChapterTitle);
        Assert.Equal(chapter.ChapterNumber, quote.ChapterNumber);
    }

    [Fact]
    public async Task CreateQuoteAsync_WithMissingChapter_ThrowsInvalidOperationException()
    {
        using var context = CreateContext();
        var user = CreateUser("missing");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        var dto = new CreateQuoteDto(Guid.NewGuid(), "Selected text", null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateQuoteAsync(user.Id, dto));
    }

    [Fact]
    public async Task GetUserQuotesAsync_ReturnsOnlyCurrentUserQuotes()
    {
        using var context = CreateContext();
        var userA = CreateUser("a");
        var userB = CreateUser("b");
        var book = CreateBook("multi");
        var chapter = CreateChapter(book, "multi", 1);
        context.Users.AddRange(userA, userB);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        await service.CreateQuoteAsync(userA.Id, new CreateQuoteDto(chapter.Id, "Quote A", null, null));
        await service.CreateQuoteAsync(userB.Id, new CreateQuoteDto(chapter.Id, "Quote B", null, null));

        var quotes = await service.GetUserQuotesAsync(userA.Id);

        Assert.Single(quotes);
        Assert.Equal("Quote A", quotes.First().SelectedText);
    }

    [Fact]
    public async Task GetQuoteByIdAsync_WhenOwnedByOtherUser_ReturnsNull()
    {
        using var context = CreateContext();
        var userA = CreateUser("a");
        var userB = CreateUser("b");
        var book = CreateBook("private");
        var chapter = CreateChapter(book, "private", 1);
        context.Users.AddRange(userA, userB);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        var created = await service.CreateQuoteAsync(userA.Id, new CreateQuoteDto(chapter.Id, "Private", null, null));

        var quote = await service.GetQuoteByIdAsync(created.Id, userB.Id);

        Assert.Null(quote);
    }

    [Fact]
    public async Task DeleteQuoteAsync_WhenOwned_DeletesAndReturnsTrue()
    {
        using var context = CreateContext();
        var user = CreateUser("del");
        var book = CreateBook("del");
        var chapter = CreateChapter(book, "del", 1);
        context.Users.Add(user);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        var created = await service.CreateQuoteAsync(user.Id, new CreateQuoteDto(chapter.Id, "To delete", null, null));

        var deleted = await service.DeleteQuoteAsync(created.Id, user.Id);
        var notFound = await service.GetQuoteByIdAsync(created.Id, user.Id);

        Assert.True(deleted);
        Assert.Null(notFound);
    }

    [Fact]
    public async Task DeleteQuoteAsync_WhenOwnedByOtherUser_ReturnsFalse()
    {
        using var context = CreateContext();
        var userA = CreateUser("a");
        var userB = CreateUser("b");
        var book = CreateBook("protected");
        var chapter = CreateChapter(book, "protected", 1);
        context.Users.AddRange(userA, userB);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        var created = await service.CreateQuoteAsync(userA.Id, new CreateQuoteDto(chapter.Id, "Protected", null, null));

        var deleted = await service.DeleteQuoteAsync(created.Id, userB.Id);

        Assert.False(deleted);
    }

    [Fact]
    public async Task UpdateQuoteAsync_UpdatesNoteOnly()
    {
        using var context = CreateContext();
        var user = CreateUser("upd");
        var book = CreateBook("upd");
        var chapter = CreateChapter(book, "upd", 1);
        context.Users.Add(user);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        var created = await service.CreateQuoteAsync(user.Id, new CreateQuoteDto(chapter.Id, "Text", "Context", "Old note"));

        var updated = await service.UpdateQuoteAsync(created.Id, user.Id, new UpdateQuoteDto("New note"));

        Assert.NotNull(updated);
        Assert.Equal("New note", updated.Note);
        Assert.Equal("Text", updated.SelectedText);
    }

    [Fact]
    public async Task GetQuotesByBookAsync_ReturnsOnlyQuotesForBook()
    {
        using var context = CreateContext();
        var user = CreateUser("book");
        var bookA = CreateBook("A");
        var bookB = CreateBook("B");
        var chapterA = CreateChapter(bookA, "A", 1);
        var chapterB = CreateChapter(bookB, "B", 1);
        context.Users.Add(user);
        context.Books.AddRange(bookA, bookB);
        context.Chapters.AddRange(chapterA, chapterB);
        await context.SaveChangesAsync();

        var service = new QuoteService(context);
        await service.CreateQuoteAsync(user.Id, new CreateQuoteDto(chapterA.Id, "Quote A", null, null));
        await service.CreateQuoteAsync(user.Id, new CreateQuoteDto(chapterB.Id, "Quote B", null, null));

        var quotes = await service.GetQuotesByBookAsync(bookA.Id, user.Id);

        Assert.Single(quotes);
        Assert.Equal("Quote A", quotes.First().SelectedText);
    }
}
