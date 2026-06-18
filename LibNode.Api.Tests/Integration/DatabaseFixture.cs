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
        // Динамический TRUNCATE всех таблиц (кроме истории миграций), чтобы список
        // не устаревал при добавлении новых сущностей.
        await context.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE
                stmt text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO stmt
                FROM pg_tables
                WHERE schemaname = 'public'
                  AND tablename <> '__EFMigrationsHistory';
                IF stmt IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || stmt || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
        """);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
