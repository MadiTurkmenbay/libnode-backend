using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class CollectionServiceTests
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
            Title = $"Book {suffix}",
        };
    }

    [Fact]
    public async Task AddBookToCollectionAsync_WhenAlreadyInTarget_DoesNotDuplicate()
    {
        using var context = CreateContext();
        var user = CreateUser("dup");
        var book = CreateBook("dup");
        var collection = new UserCollection { Id = Guid.NewGuid(), UserId = user.Id, Name = "Collection dup" };
        context.Users.Add(user);
        context.Books.Add(book);
        context.UserCollections.Add(collection);
        await context.SaveChangesAsync();

        var service = new CollectionService(context);
        await service.AddBookToCollectionAsync(collection.Id, book.Id, user.Id);
        await service.AddBookToCollectionAsync(collection.Id, book.Id, user.Id);

        Assert.Equal(1, context.CollectionBooks.Count(cb => cb.BookId == book.Id && cb.Collection!.UserId == user.Id));
    }

    [Fact]
    public async Task MoveBookBetweenCollectionsAsync_AtomicallyRemovesFromSource()
    {
        using var context = CreateContext();
        var user = CreateUser("move");
        var book = CreateBook("move");
        var collectionA = new UserCollection { Id = Guid.NewGuid(), UserId = user.Id, Name = "A" };
        var collectionB = new UserCollection { Id = Guid.NewGuid(), UserId = user.Id, Name = "B" };
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
