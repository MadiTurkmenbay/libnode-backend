using System.Net;
using System.Net.Http.Json;
using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class BooksApiTests
{
    private readonly ApiFactory _factory;
    private readonly IServiceProvider _services;

    public BooksApiTests(DatabaseFixture fixture)
    {
        _factory = new ApiFactory();
        _services = fixture.Services;
    }

    private static async Task<Book[]> SeedBooksAsync(AppDbContext context)
    {
        // Очищаем таблицы для изоляции каталоговых тестов от данных других интеграционных тестов.
        await context.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "Chapters", "Books" CASCADE;
        """);

        var now = DateTime.UtcNow;
        var books = new[]
        {
            new Book { Id = Guid.NewGuid(), Title = "Alpha", CreatedAt = now.AddHours(-2), UpdatedAt = now.AddHours(-2) },
            new Book { Id = Guid.NewGuid(), Title = "Beta", CreatedAt = now.AddHours(-1), UpdatedAt = now.AddHours(-1) },
            new Book { Id = Guid.NewGuid(), Title = "Gamma", CreatedAt = now, UpdatedAt = now },
        };
        context.Books.AddRange(books);
        await context.SaveChangesAsync();
        return books;
    }

    [Fact]
    public async Task GetAll_Default_ReturnsCursorPagedResult()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedBooksAsync(context);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/books?limit=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CursorStringPagedResult<BookDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task GetAll_WithSortByTitle_ReturnsCursorPagedResult()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedBooksAsync(context);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/books?limit=2&sortBy=Title&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CursorStringPagedResult<BookDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.True(result.HasMore);
        Assert.Equal("Alpha", result.Items[0].Title);
        Assert.Equal("Beta", result.Items[1].Title);
    }

    [Fact]
    public async Task GetAll_WithNextCursor_ReturnsNextPage()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var books = await SeedBooksAsync(context);

        var client = _factory.CreateClient();
        var firstResponse = await client.GetAsync("/api/books?limit=1&sortBy=Title&sortDirection=asc");
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<CursorStringPagedResult<BookDto>>();
        Assert.NotNull(firstPage);
        Assert.NotNull(firstPage.NextCursor);

        var secondResponse = await client.GetAsync($"/api/books?limit=10&sortBy=Title&sortDirection=asc&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<CursorStringPagedResult<BookDto>>();
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal("Beta", secondPage.Items[0].Title);
        Assert.Equal("Gamma", secondPage.Items[1].Title);
    }

    [Fact]
    public async Task GetAll_InvalidCursor_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/books?cursor=invalid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
