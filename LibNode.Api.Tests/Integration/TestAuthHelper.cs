using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace LibNode.Api.Tests.Integration;

public static class TestAuthHelper
{
    public static string GenerateToken(Guid userId, string role = "User", string? email = null)
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
            new Claim(ClaimTypes.Email, email ?? $"user_{userId}@test.local")
        };

        var token = new JwtSecurityToken(
            issuer: "LibNode.Api",
            audience: "LibNode.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
