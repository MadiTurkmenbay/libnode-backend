using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibNode.Api.Tests.Integration;

[Collection("DatabaseCollection")]
public class AuthServiceTests
{
    private readonly ApiFactory _factory;
    private readonly IServiceProvider _services;

    public AuthServiceTests(DatabaseFixture fixture)
    {
        _factory = new ApiFactory();
        _services = fixture.Services;
    }

    private IAuthService GetAuthService(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IAuthService>();

    private static CreateUserDto NewUser(out string email, out string password)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        email = $"reg_{id}@test.local";
        password = "password123";
        return new CreateUserDto($"reg_{id}", email, password);
    }

    // ── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsTokenWithNameIdentifierAndSstamp()
    {
        using var scope = _services.CreateScope();
        var auth = GetAuthService(scope);
        var dto = NewUser(out _, out _);

        var result = await auth.RegisterAsync(dto);

        Assert.NotNull(result.Token);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        // sub -> NameIdentifier через JwtSecurityToken; проверяем оба «сырых» claim'а.
        var sub = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        Assert.Equal(result.User.Id.ToString(), sub);

        var sstamp = jwt.Claims.First(c => c.Type == AuthService.SecurityStampClaimType).Value;
        Assert.False(string.IsNullOrEmpty(sstamp));

        using var verifyScope = _services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Users.AsNoTracking().FirstAsync(u => u.Id == result.User.Id);
        Assert.Equal(stored.SecurityStamp, sstamp);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_Throws()
    {
        using var scope = _services.CreateScope();
        var auth = GetAuthService(scope);
        var dto = NewUser(out var email, out _);
        await auth.RegisterAsync(dto);

        var dup = new CreateUserDto(dto.Username, $"other_{Guid.NewGuid():N}@test.local", "password123");

        using var scope2 = _services.CreateScope();
        var auth2 = GetAuthService(scope2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth2.RegisterAsync(dup));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Throws()
    {
        using var scope = _services.CreateScope();
        var auth = GetAuthService(scope);
        var dto = NewUser(out var email, out _);
        await auth.RegisterAsync(dto);

        var dup = new CreateUserDto($"other_{Guid.NewGuid():N}"[..20], email, "password123");

        using var scope2 = _services.CreateScope();
        var auth2 = GetAuthService(scope2);
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth2.RegisterAsync(dup));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CorrectPassword_ReturnsToken()
    {
        using var scope = _services.CreateScope();
        var auth = GetAuthService(scope);
        var dto = NewUser(out var email, out var password);
        await auth.RegisterAsync(dto);

        var result = await auth.LoginAsync(new LoginDto(email, password));

        Assert.NotNull(result.Token);
        Assert.Equal(email, result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        using var scope = _services.CreateScope();
        var auth = GetAuthService(scope);
        var dto = NewUser(out var email, out _);
        await auth.RegisterAsync(dto);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => auth.LoginAsync(new LoginDto(email, "wrong-password")));
    }

    [Fact]
    public async Task LoginAsync_NonexistentUser_ThrowsUnauthorized()
    {
        using var scope = _services.CreateScope();
        var auth = GetAuthService(scope);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => auth.LoginAsync(new LoginDto($"missing_{Guid.NewGuid():N}@test.local", "password123")));
    }

    [Fact]
    public async Task Login_NonexistentUser_HttpReturns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto($"missing_{Guid.NewGuid():N}@test.local", "password123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Revocation (SecurityStamp) ──────────────────────────────────────────

    [Fact]
    public async Task FreshToken_IsAcceptedByProtectedEndpoint()
    {
        string token;
        using (var scope = _services.CreateScope())
        {
            var auth = GetAuthService(scope);
            var dto = NewUser(out _, out _);
            token = (await auth.RegisterAsync(dto)).Token;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/quotes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangingSecurityStamp_InvalidatesOldToken()
    {
        Guid userId;
        string token;
        using (var scope = _services.CreateScope())
        {
            var auth = GetAuthService(scope);
            var dto = NewUser(out _, out _);
            var result = await auth.RegisterAsync(dto);
            userId = result.User.Id;
            token = result.Token;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Старый токен валиден до ротации метки.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/quotes")).StatusCode);

        // Ротация SecurityStamp (как при смене пароля/бане) → старый токен отозван.
        using (var scope = _services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/quotes")).StatusCode);
    }
}
