using LibNode.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly ApiFactory _factory = new ApiFactory();

    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        // Очищаем данные от предыдущих запусков тестового процесса, сохраняя схему.
        await context.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "BookTag", "BookCategory", "CollectionBooks", "ChapterLikes", "ReadingProgresses", "UserCollections", "Chapters", "Books", "Users", "Tags", "Categories" CASCADE;
        """);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
