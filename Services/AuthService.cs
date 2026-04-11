using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LibNode.Api.Services;

/// <summary>
/// Сервис аутентификации: регистрация, логин, генерация JWT.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> RegisterAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        // Проверка уникальности email
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            throw new InvalidOperationException("Пользователь с таким email уже существует.");

        // Проверка уникальности username
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username, ct))
            throw new InvalidOperationException("Пользователь с таким именем уже существует.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = GenerateJwtToken(user);
        return new AuthResponseDto(token, MapToDto(user));
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Неверный email или пароль.");

        var token = GenerateJwtToken(user);
        return new AuthResponseDto(token, MapToDto(user));
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Генерация JWT токена с claims: sub, email, role, jti.
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiresMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "1440");

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapToDto(User user) =>
        new(user.Id, user.Username, user.Email, user.Role);
}
