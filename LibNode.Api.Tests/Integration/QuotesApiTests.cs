using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class QuotesApiTests
{
    private readonly ApiFactory _factory;
    private readonly IServiceProvider _services;

    public QuotesApiTests(DatabaseFixture fixture)
    {
        _factory = new ApiFactory();
        _services = fixture.Services;
    }

    private static async Task<(Book book, Chapter chapter, User user)> SeedAsync(AppDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"quote_user_{Guid.NewGuid():N}",
            Email = $"quote_user_{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash"
        };
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = "Quote Book"
        };
        var chapter = new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            Title = "Quote Chapter",
            Content = "Some content for quote selection",
            ChapterNumber = 1
        };

        context.Users.Add(user);
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();
        return (book, chapter, user);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHelper.GenerateToken(userId));
        return client;
    }

    [Fact]
    public async Task CreateQuote_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteDto(Guid.NewGuid(), "Text", null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuote_Authenticated_ReturnsCreated()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, chapter, user) = await SeedAsync(context);

        var client = CreateAuthenticatedClient(user.Id);
        var response = await client.PostAsJsonAsync("/api/quotes", new CreateQuoteDto(chapter.Id, "Selected text", "Context", "Note"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<QuoteDto>();
        Assert.NotNull(quote);
        Assert.Equal("Selected text", quote.SelectedText);
        Assert.Equal(chapter.Id, quote.ChapterId);
    }

    [Fact]
    public async Task GetMyQuotes_Authenticated_ReturnsUserQuotes()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, chapter, user) = await SeedAsync(context);

        context.Quotes.Add(new Quote
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ChapterId = chapter.Id,
            BookId = chapter.BookId,
            SelectedText = "Saved quote",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var client = CreateAuthenticatedClient(user.Id);
        var response = await client.GetAsync("/api/quotes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quotes = await response.Content.ReadFromJsonAsync<List<QuoteDto>>();
        Assert.NotNull(quotes);
        Assert.Single(quotes);
        Assert.Equal("Saved quote", quotes[0].SelectedText);
    }

    [Fact]
    public async Task GetQuoteById_OwnedByOtherUser_ReturnsNotFound()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, chapter, userA) = await SeedAsync(context);
        var userB = new User
        {
            Id = Guid.NewGuid(),
            Username = $"other_quote_user_{Guid.NewGuid():N}",
            Email = $"other_quote_user_{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash"
        };
        context.Users.Add(userB);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            UserId = userA.Id,
            ChapterId = chapter.Id,
            BookId = chapter.BookId,
            SelectedText = "Private quote",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Quotes.Add(quote);
        await context.SaveChangesAsync();

        var client = CreateAuthenticatedClient(userB.Id);
        var response = await client.GetAsync($"/api/quotes/{quote.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuote_Authenticated_DeletesAndReturnsNoContent()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, chapter, user) = await SeedAsync(context);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ChapterId = chapter.Id,
            BookId = chapter.BookId,
            SelectedText = "To delete",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Quotes.Add(quote);
        await context.SaveChangesAsync();

        var client = CreateAuthenticatedClient(user.Id);
        var response = await client.DeleteAsync($"/api/quotes/{quote.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var deleted = await context.Quotes.FirstOrDefaultAsync(q => q.Id == quote.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetQuotesByBook_ReturnsOnlyQuotesForBook()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (book, chapter, user) = await SeedAsync(context);

        context.Quotes.Add(new Quote
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ChapterId = chapter.Id,
            BookId = chapter.BookId,
            SelectedText = "Book quote",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var client = CreateAuthenticatedClient(user.Id);
        var response = await client.GetAsync($"/api/quotes/by-book/{book.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quotes = await response.Content.ReadFromJsonAsync<List<QuoteDto>>();
        Assert.NotNull(quotes);
        Assert.Single(quotes);
        Assert.Equal("Book quote", quotes[0].SelectedText);
    }
}
