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

        var service = new BookService(context);
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

        var service = new BookService(context);
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

        // UpdatedAt перезаписывается при добавлении, поэтому явно обновляем после сохранения.
        books[0].UpdatedAt = now.AddHours(-1);
        books[1].UpdatedAt = now.AddHours(-2);
        await context.SaveChangesAsync();

        var service = new BookService(context);
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

        var service = new BookService(context);
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

        var service = new BookService(context);
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
        var service = new BookService(context);

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

        var service = new BookService(context);
        var result = await service.GetAllAsync(new GetBooksQueryDto
        {
            Limit = 1,
            Types = [BookType.Japan],
        });

        Assert.Single(result.Items);
        Assert.True(result.HasMore);
        Assert.Equal(books[2].Id, result.Items[0].Id);
    }
}
