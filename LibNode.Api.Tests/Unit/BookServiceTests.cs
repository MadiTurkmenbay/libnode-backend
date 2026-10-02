using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class BookServiceTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Book CreateBook(string title, DateTime createdAt, DateTime updatedAt, BookType type = BookType.Japan)
    {
        return new Book
        {
            Id = Guid.NewGuid(),
            Title = title,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Type = type,
        };
    }

    [Fact]
    public async Task GetAllAsync_DefaultSort_ReturnsCursorPagedResult()
    {
        await using var context = CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var books = new[]
        {
            CreateBook("Alpha", now.AddHours(-2), now.AddHours(-2)),
            CreateBook("Beta", now.AddHours(-1), now.AddHours(-1)),
            CreateBook("Gamma", now, now),
        };
        context.Books.AddRange(books);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.GetAllAsync(new GetBooksQueryDto { Limit = 2 });

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.NotNull(result.NextCursor);
        Assert.Equal(books[1].Id, result.Items[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_CreatedAtAsc_ReturnsCursorPagedResult()
    {
        await using var context = CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var books = new[]
        {
            CreateBook("Alpha", now.AddHours(-2), now.AddHours(-2)),
            CreateBook("Beta", now.AddHours(-1), now.AddHours(-1)),
            CreateBook("Gamma", now, now),
        };
        context.Books.AddRange(books);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 2,
            SortBy = BookSortBy.CreatedAt,
            SortDirection = "asc",
        });

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.Equal(books[0].Id, result.Items[0].Id);
        Assert.Equal(books[1].Id, result.Items[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_UpdatedAtDesc_ReturnsCursorPagedResult()
    {
        await using var context = CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var books = new[]
        {
            CreateBook("Alpha", now.AddHours(-2), now),
            CreateBook("Beta", now.AddHours(-1), now),
        };
        context.Books.AddRange(books);
        await context.SaveChangesAsync();

        // Явно расставляем UpdatedAt после сохранения, чтобы порядок теста был очевидным.
        books[0].UpdatedAt = now.AddHours(-1);
        books[1].UpdatedAt = now.AddHours(-2);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 1,
            SortBy = BookSortBy.UpdatedAt,
            SortDirection = "desc",
        });

        Assert.Single(result.Items);
        Assert.True(result.HasMore);
        Assert.Equal(books[0].Id, result.Items[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_TitleAsc_ReturnsCursorPagedResult()
    {
        await using var context = CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var books = new[]
        {
            CreateBook("Beta", now, now),
            CreateBook("Alpha", now, now),
            CreateBook("Gamma", now, now),
        };
        context.Books.AddRange(books);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 2,
            SortBy = BookSortBy.Title,
            SortDirection = "asc",
        });

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.Equal(books[1].Id, result.Items[0].Id); // Alpha
        Assert.Equal(books[0].Id, result.Items[1].Id); // Beta
    }

    [Fact]
    public async Task GetAllAsync_TitleWithPipeChar_ParsesCursorCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var book1 = CreateBook("A|B|C", now, now);
        var book2 = CreateBook("Z", now, now);
        context.Books.AddRange(book1, book2);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var firstPage = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 1,
            SortBy = BookSortBy.Title,
            SortDirection = "asc",
        });

        Assert.Single(firstPage.Items);
        Assert.True(firstPage.HasMore);
        Assert.Equal(book1.Id, firstPage.Items[0].Id);

        var secondPage = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 10,
            SortBy = BookSortBy.Title,
            SortDirection = "asc",
            Cursor = firstPage.NextCursor,
        });

        Assert.Single(secondPage.Items);
        Assert.False(secondPage.HasMore);
        Assert.Equal(book2.Id, secondPage.Items[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_InvalidCursor_ThrowsArgumentException()
    {
        await using var context = CreateInMemoryContext();
        var service = new BookService(context, new FakeStorageService());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAllAsync(new GetBooksQueryDto { Cursor = "not-a-cursor" }));
    }

    [Fact]
    public async Task GetAllAsync_FiltersWithCursor_ReturnsCursorPagedResult()
    {
        await using var context = CreateInMemoryContext();
        var now = DateTime.UtcNow;
        var books = new[]
        {
            CreateBook("Alpha", now.AddHours(-2), now.AddHours(-2), BookType.Japan),
            CreateBook("Beta", now.AddHours(-1), now.AddHours(-1), BookType.Korea),
            CreateBook("Gamma", now, now, BookType.Japan),
        };
        context.Books.AddRange(books);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 1,
            Types = [BookType.Japan],
        });

        Assert.Single(result.Items);
        Assert.True(result.HasMore);
        Assert.Equal(books[2].Id, result.Items[0].Id);
    }

    [Fact]
    public async Task UpdateCoverKeysAsync_ReplacesKeysAndReturnsPreviousKeys()
    {
        await using var context = CreateInMemoryContext();
        var book = CreateBook("Cover", DateTime.UtcNow, DateTime.UtcNow);
        book.CoverUrl = "covers/old.webp";
        book.CoverThumbUrl = "covers/thumbs/old.webp";
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var oldKeys = await service.UpdateCoverKeysAsync(book.Id, "covers/new.webp", "covers/thumbs/new.webp");

        Assert.NotNull(oldKeys);
        Assert.Equal("covers/old.webp", oldKeys.Value.CoverKey);
        Assert.Equal("covers/thumbs/old.webp", oldKeys.Value.CoverThumbKey);
        Assert.Equal("covers/new.webp", book.CoverUrl);
        Assert.Equal("covers/thumbs/new.webp", book.CoverThumbUrl);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRatingStats()
    {
        await using var context = CreateInMemoryContext();
        var book = CreateBook("Rated", DateTime.UtcNow, DateTime.UtcNow);
        var userA = new User { Id = Guid.NewGuid(), Username = "rater-a", Email = "rater-a@test.local", PasswordHash = "hash" };
        var userB = new User { Id = Guid.NewGuid(), Username = "rater-b", Email = "rater-b@test.local", PasswordHash = "hash" };
        context.Books.Add(book);
        context.Users.AddRange(userA, userB);
        context.BookRatings.AddRange(
            new BookRating { BookId = book.Id, UserId = userA.Id, Value = 4 },
            new BookRating { BookId = book.Id, UserId = userB.Id, Value = 2 });
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.GetAllAsync(new GetBooksQueryDto { Limit = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal(3, dto.AverageRating);
        Assert.Equal(2, dto.RatingCount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsRatingStats()
    {
        await using var context = CreateInMemoryContext();
        var book = CreateBook("Before", DateTime.UtcNow, DateTime.UtcNow);
        var user = new User { Id = Guid.NewGuid(), Username = "rater", Email = "rater@test.local", PasswordHash = "hash" };
        context.Books.Add(book);
        context.Users.Add(user);
        context.BookRatings.Add(new BookRating { BookId = book.Id, UserId = user.Id, Value = 5 });
        await context.SaveChangesAsync();

        var service = new BookService(context, new FakeStorageService());
        var result = await service.UpdateAsync(book.Id, new UpdateBookDto(
            "After",
            "Description",
            null,
            BookType.Korea,
            OriginalStatus.Ongoing,
            TranslationStatus.Ongoing));

        Assert.NotNull(result);
        Assert.Equal(5, result.AverageRating);
        Assert.Equal(1, result.RatingCount);
    }
}
