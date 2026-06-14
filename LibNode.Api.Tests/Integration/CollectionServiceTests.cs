using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class CollectionServiceTests
{
    private readonly IServiceProvider _services;

    public CollectionServiceTests(DatabaseFixture fixture)
    {
        _services = fixture.Services;
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

    private static async Task<(User user, Book book, UserCollection collection)> SeedUserBookAndCollectionAsync(AppDbContext context, string suffix)
    {
        var user = CreateUser(suffix);
        var book = CreateBook(suffix);
        var collection = new UserCollection
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = $"Collection {suffix}",
        };
        context.Users.Add(user);
        context.Books.Add(book);
        context.UserCollections.Add(collection);
        await context.SaveChangesAsync();
        return (user, book, collection);
    }

    [Fact]
    public async Task AddBookToTargetTwice_IsIdempotent()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ICollectionService>();

        var (user, book, collection) = await SeedUserBookAndCollectionAsync(context, "idempotent");

        await service.AddBookToCollectionAsync(collection.Id, book.Id, user.Id);
        await service.AddBookToCollectionAsync(collection.Id, book.Id, user.Id);

        var count = await context.CollectionBooks
            .CountAsync(cb => cb.BookId == book.Id && cb.Collection!.UserId == user.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MoveBookBetweenCollections_AtomicallyPreservesOneBookOneUser()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ICollectionService>();

        var user = CreateUser("move");
        var book = CreateBook("move");
        var collectionA = new UserCollection { Id = Guid.NewGuid(), UserId = user.Id, Name = "Collection A" };
        var collectionB = new UserCollection { Id = Guid.NewGuid(), UserId = user.Id, Name = "Collection B" };
        context.Users.Add(user);
        context.Books.Add(book);
        context.UserCollections.Add(collectionA);
        context.UserCollections.Add(collectionB);
        await context.SaveChangesAsync();

        await service.AddBookToCollectionAsync(collectionA.Id, book.Id, user.Id);
        await service.AddBookToCollectionAsync(collectionB.Id, book.Id, user.Id);

        var link = await context.CollectionBooks
            .FirstOrDefaultAsync(cb => cb.BookId == book.Id && cb.Collection!.UserId == user.Id);
        Assert.NotNull(link);
        Assert.Equal(collectionB.Id, link.CollectionId);

        var inA = await context.CollectionBooks
            .AnyAsync(cb => cb.CollectionId == collectionA.Id && cb.BookId == book.Id);
        Assert.False(inA);
    }

    [Fact]
    public async Task AddBookToOtherUserCollection_ThrowsUnauthorizedAccess()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<ICollectionService>();

        var owner = CreateUser("owner");
        var intruder = CreateUser("intruder");
        var book = CreateBook("owner");
        var collection = new UserCollection { Id = Guid.NewGuid(), UserId = owner.Id, Name = "Owner Collection" };
        context.Users.Add(owner);
        context.Users.Add(intruder);
        context.Books.Add(book);
        context.UserCollections.Add(collection);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AddBookToCollectionAsync(collection.Id, book.Id, intruder.Id));
    }
}
