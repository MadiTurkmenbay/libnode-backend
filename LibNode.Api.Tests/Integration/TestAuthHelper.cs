using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.IdentityModel.Tokens;

namespace LibNode.Api.Tests.Integration;

public static class TestAuthHelper
{
    /// <summary>
    /// Генерирует JWT в формате, который принимает production-валидация (OnTokenValidated):
    /// несёт NameIdentifier, Role и обязательный sstamp-claim (<see cref="User.SecurityStamp"/>).
    /// Для прохождения revocation-проверки в БД должен существовать соответствующий не забаненный
    /// пользователь с тем же SecurityStamp и Role — используйте <see cref="SeedUserAndTokenAsync"/>.
    /// </summary>
    public static string GenerateToken(
        Guid userId,
        string role = "User",
        string? email = null,
        string? securityStamp = null)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JwtSettings__Key")
            ?? "placeholder-jwt-signing-key-for-verify-only";

        var key = System.Text.Encoding.UTF8.GetBytes(jwtKey);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Email, email ?? $"user_{userId}@test.local"),
            new Claim(AuthService.SecurityStampClaimType, securityStamp ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: "LibNode.Api",
            audience: "LibNode.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Создаёт не забаненного пользователя в тестовой БД и возвращает (userId, token), где токен
    /// несёт совпадающие NameIdentifier/Role/sstamp — то есть проходит production revocation-проверку.
    /// </summary>
    public static async Task<(Guid userId, string token)> SeedUserAndTokenAsync(
        AppDbContext db,
        string role = "User",
        CancellationToken ct = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"auth_user_{Guid.NewGuid():N}",
            Email = $"auth_user_{Guid.NewGuid():N}@test.local",
            PasswordHash = "hash",
            Role = role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = GenerateToken(user.Id, role, user.Email, user.SecurityStamp);
        return (user.Id, token);
    }
}
