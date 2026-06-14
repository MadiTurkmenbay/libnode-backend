using LibNode.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class MigrationTests
{
    private readonly IServiceProvider _services;

    public MigrationTests(DatabaseFixture fixture)
    {
        _services = fixture.Services;
    }

    [Fact]
    public async Task NoPendingMigrations_AfterApplying()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        var pending = await context.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);
    }

    [Fact]
    public async Task GenerateCreateScript_ContainsExpectedTables()
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var script = context.Database.GenerateCreateScript();

        Assert.NotEmpty(script);
        Assert.Contains("Books", script);
        Assert.Contains("Chapters", script);
        Assert.Contains("Users", script);
        Assert.Contains("ReadingProgresses", script);
        Assert.Contains("UserCollections", script);
        Assert.Contains("CollectionBooks", script);
    }
}
