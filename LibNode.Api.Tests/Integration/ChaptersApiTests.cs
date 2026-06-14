using System.Net;
using System.Net.Http.Json;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class ChaptersApiTests
{
    private readonly ApiFactory _factory;
    private readonly IServiceProvider _services;

    public ChaptersApiTests(DatabaseFixture fixture)
    {
        _factory = new ApiFactory();
        _services = fixture.Services;
    }

    private static async Task<(Book book, Chapter[] chapters)> SeedBookWithChaptersAsync(AppDbContext context)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = "Neighbor Book"
        };
        var chapters = new[]
        {
            new Chapter { Id = Guid.NewGuid(), BookId = book.Id, Title = "Chapter 1", Content = "...", ChapterNumber = 1 },
            new Chapter { Id = Guid.NewGuid(), BookId = book.Id, Title = "Chapter 2", Content = "...", ChapterNumber = 2 },
            new Chapter { Id = Guid.NewGuid(), BookId = book.Id, Title = "Chapter 5", Content = "...", ChapterNumber = 5 },
        };
        context.Books.Add(book);
        context.Chapters.AddRange(chapters);
        await context.SaveChangesAsync();
        return (book, chapters);
    }

    [Fact]
    public async Task GetById_ReturnsNeighborFields()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var (_, chapters) = await SeedBookWithChaptersAsync(context);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/chapters/{chapters[2].Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chapter = await response.Content.ReadFromJsonAsync<ChapterDetailDto>();
        Assert.NotNull(chapter);
        Assert.Equal(chapters[1].Id, chapter.PreviousChapterId);
        Assert.Null(chapter.NextChapterId);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/chapters/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
