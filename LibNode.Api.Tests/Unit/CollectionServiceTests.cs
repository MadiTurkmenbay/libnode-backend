using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Tests.Unit;

public class CollectionServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(User user, Book book, UserCollection collection)> SeedUserBookAndCollectionAsync(AppDbContext context, string suffix)
    {
        var user = new User
        {
            Username = $"user_{suffix}",
            Email = $"user_{suffix}@test.local",
            PasswordHash = "hash"
        };
        var book = new Book { Title = $"Book {suffix}" };
        var collection = new UserCollection { UserId = user.Id, Name = $"Collection {suffix}" };
        context.Users.Add(user);
        context.Books.Add(book);
        context.UserCollections.Add(collection);
        await context.SaveChangesAsync();
        return (user, book, collection);
    }

    [Fact]
    public async Task AddBookToCollectionAsync_WhenAlreadyInTarget_DoesNotDuplicate()
    {
        using var context = CreateContext();
        var (user, book, collection) = await SeedUserBookAndCollectionAsync(context, "dup");
        var service = new CollectionService(context);

        await service.AddBookToCollectionAsync(collection.Id, book.Id, user.Id);
        await service.AddBookToCollectionAsync(collection.Id, book.Id, user.Id);

        Assert.Equal(1, context.CollectionBooks.Count(cb => cb.BookId == book.Id && cb.Collection!.UserId == user.Id));
    }

    [Fact]
    public async Task MoveBookBetweenCollectionsAsync_AtomicallyRemovesFromSource()
    {
        using var context = CreateContext();
        var user = new User
        {
            Username = "move_user",
            Email = "move_user@test.local",
            PasswordHash = "hash"
        };
        var book = new Book { Title = "Move Book" };
        var collectionA = new UserCollection { UserId = user.Id, Name = "A" };
        var collectionB = new UserCollection { UserId = user.Id, Name = "B" };
        context.Users.Add(user);
        context.Books.Add(book);
        context.UserCollections.Add(collectionA);
        context.UserCollections.Add(collectionB);
        await context.SaveChangesAsync();

        var service = new CollectionService(context);
        await service.AddBookToCollectionAsync(collectionA.Id, book.Id, user.Id);
        await service.AddBookToCollectionAsync(collectionB.Id, book.Id, user.Id);

        var link = context.CollectionBooks
            .FirstOrDefault(cb => cb.BookId == book.Id && cb.Collection!.UserId == user.Id);
        Assert.NotNull(link);
        Assert.Equal(collectionB.Id, link.CollectionId);
        Assert.False(context.CollectionBooks.Any(cb => cb.CollectionId == collectionA.Id && cb.BookId == book.Id));
    }
}
