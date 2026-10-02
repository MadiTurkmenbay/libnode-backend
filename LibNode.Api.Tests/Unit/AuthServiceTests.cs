using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class AuthServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "unit-test-signing-key-32-bytes-minimum-1234567890",
            ["JwtSettings:Issuer"] = "LibNode.Api.Tests",
            ["JwtSettings:Audience"] = "LibNode.Client.Tests",
            ["JwtSettings:ExpiresInMinutes"] = "60",
        })
        .Build();

    [Fact]
    public async Task UpdateAvatarKeysAsync_ReplacesKeysAndReturnsPreviousKeys()
    {
        await using var context = CreateContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "avatar-user",
            Email = "avatar-user@test.local",
            PasswordHash = "hash",
            AvatarUrl = "avatars/old.webp",
            AvatarThumbUrl = "avatars/thumbs/old.webp",
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, CreateConfig(), new FakeStorageService());
        var oldKeys = await service.UpdateAvatarKeysAsync(user.Id, "avatars/new.webp", "avatars/thumbs/new.webp");

        Assert.NotNull(oldKeys);
        Assert.Equal("avatars/old.webp", oldKeys.Value.AvatarKey);
        Assert.Equal("avatars/thumbs/old.webp", oldKeys.Value.AvatarThumbKey);
        Assert.Equal("avatars/new.webp", user.AvatarUrl);
        Assert.Equal("avatars/thumbs/new.webp", user.AvatarThumbUrl);
    }
}
