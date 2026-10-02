using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class AppDbContextTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAddingEntity_PreservesExplicitUpdatedAt()
    {
        await using var context = CreateContext();
        var createdAt = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 02, 03, 04, 05, 06, DateTimeKind.Utc);
        var book = new Book
        {
            Title = "Imported book",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };

        context.Books.Add(book);
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, book.CreatedAt);
        Assert.Equal(updatedAt, book.UpdatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenAddingEntity_SetsDefaultUpdatedAt()
    {
        await using var context = CreateContext();
        var book = new Book { Title = "New book" };

        context.Books.Add(book);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, book.CreatedAt);
        Assert.NotEqual(default, book.UpdatedAt);
    }
}
