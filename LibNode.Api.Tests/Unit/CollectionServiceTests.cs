using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
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

        var service = new CollectionService(context, new FakeStorageService());
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

        var service = new CollectionService(context, new FakeStorageService());
        await service.AddBookToCollectionAsync(collectionA.Id, book.Id, user.Id);
        await service.AddBookToCollectionAsync(collectionB.Id, book.Id, user.Id);

        var link = context.CollectionBooks
            .FirstOrDefault(cb => cb.BookId == book.Id && cb.Collection!.UserId == user.Id);
        Assert.NotNull(link);
        Assert.Equal(collectionB.Id, link.CollectionId);
        Assert.False(context.CollectionBooks.Any(cb => cb.CollectionId == collectionA.Id && cb.BookId == book.Id));
    }

    [Fact]
    public async Task RenameCollectionAsync_PreservesIdentityAndMembership()
    {
        using var context = CreateContext();
        var user = CreateUser("rename");
        var book = CreateBook("rename");
        var collection = new UserCollection { UserId = user.Id, Name = "Before" };
        context.Users.Add(user);
        context.Books.Add(book);
        context.UserCollections.Add(collection);
        await context.SaveChangesAsync();
        context.CollectionBooks.Add(new CollectionBook { CollectionId = collection.Id, BookId = book.Id });
        await context.SaveChangesAsync();
        var createdAt = collection.CreatedAt;
        var service = new CollectionService(context, new FakeStorageService());

        var result = await service.RenameCollectionAsync(collection.Id, user.Id, new CreateCollectionDto { Name = "  After  " });

        Assert.NotNull(result);
        Assert.Equal(collection.Id, result.Id);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal("After", result.Name);
        Assert.Equal(1, result.BookCount);
        Assert.Equal("After", (await context.UserCollections.FindAsync(collection.Id))!.Name);
        Assert.Single(context.CollectionBooks);
        Assert.Single(context.Books);
    }

    [Fact]
    public async Task DeleteCollectionAsync_PreservesBookAndOtherUsersMembership()
    {
        using var context = CreateContext();
        var owner = CreateUser("delete");
        var other = CreateUser("other");
        var book = CreateBook("delete");
        var target = new UserCollection { UserId = owner.Id, Name = "Target" };
        var empty = new UserCollection { UserId = owner.Id, Name = "Empty" };
        var independent = new UserCollection { UserId = other.Id, Name = "Independent" };
        context.Users.AddRange(owner, other);
        context.Books.Add(book);
        context.UserCollections.AddRange(target, empty, independent);
        await context.SaveChangesAsync();
        context.CollectionBooks.AddRange(
            new CollectionBook { CollectionId = target.Id, BookId = book.Id },
            new CollectionBook { CollectionId = independent.Id, BookId = book.Id });
        await context.SaveChangesAsync();
        var service = new CollectionService(context, new FakeStorageService());

        Assert.True(await service.DeleteCollectionAsync(target.Id, owner.Id));

        Assert.Null(await context.UserCollections.FindAsync(target.Id));
        Assert.Equal(2, await context.UserCollections.CountAsync());
        Assert.Single(context.Books);
        Assert.Equal(independent.Id, Assert.Single(context.CollectionBooks).CollectionId);
        Assert.Equal(2, await context.Users.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CollectionMutation_WhenForeignOwner_DoesNotChangeData(bool delete)
    {
        using var context = CreateContext();
        var owner = CreateUser("owner");
        var other = CreateUser("foreign");
        var collection = new UserCollection { UserId = owner.Id, Name = "Owned" };
        context.Users.AddRange(owner, other);
        context.UserCollections.Add(collection);
        await context.SaveChangesAsync();
        var service = new CollectionService(context, new FakeStorageService());

        if (delete)
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteCollectionAsync(collection.Id, other.Id));
        else
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RenameCollectionAsync(collection.Id, other.Id, new CreateCollectionDto { Name = "Wrong" }));

        Assert.Equal("Owned", Assert.Single(context.UserCollections).Name);
    }

    [Fact]
    public async Task CollectionMutation_WhenMissing_ReturnsNotFound()
    {
        using var context = CreateContext();
        var service = new CollectionService(context, new FakeStorageService());
        Assert.Null(await service.RenameCollectionAsync(Guid.NewGuid(), Guid.NewGuid(), new CreateCollectionDto { Name = "Missing" }));
        Assert.False(await service.DeleteCollectionAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCollectionByIdAsync_ReturnsPublishedChapterCountAndRatingStats()
    {
        using var context = CreateContext();
        var user = CreateUser("detail");
        var raterA = CreateUser("rater-a");
        var raterB = CreateUser("rater-b");
        var book = CreateBook("detail");
        var collection = new UserCollection { Id = Guid.NewGuid(), UserId = user.Id, Name = "Collection detail" };

        context.Users.AddRange(user, raterA, raterB);
        context.Books.Add(book);
        context.UserCollections.Add(collection);
        context.CollectionBooks.Add(new CollectionBook { CollectionId = collection.Id, BookId = book.Id });
        context.Chapters.AddRange(
            new Chapter { Id = Guid.NewGuid(), BookId = book.Id, Title = "Published", Content = "Content", ChapterNumber = 1, IsPublished = true },
            new Chapter { Id = Guid.NewGuid(), BookId = book.Id, Title = "Draft", Content = "Content", ChapterNumber = 2, IsPublished = false });
        context.BookRatings.AddRange(
            new BookRating { BookId = book.Id, UserId = raterA.Id, Value = 5 },
            new BookRating { BookId = book.Id, UserId = raterB.Id, Value = 3 });
        await context.SaveChangesAsync();

        var service = new CollectionService(context, new FakeStorageService());
        var detail = await service.GetCollectionByIdAsync(collection.Id, user.Id);

        Assert.NotNull(detail);
        var dto = Assert.Single(detail.Books);
        Assert.Equal(1, dto.ChapterCount);
        Assert.Equal(4, dto.AverageRating);
        Assert.Equal(2, dto.RatingCount);
    }
}
