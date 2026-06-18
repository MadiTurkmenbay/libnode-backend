using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace LibNode.Api.Services;

/// <summary>
/// Сервис аутентификации: регистрация, логин, генерация JWT.
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>Claim type, несущий <see cref="User.SecurityStamp"/> в JWT.</summary>
    public const string SecurityStampClaimType = "sstamp";

    // Фиксированный BCrypt-хэш для подавления timing-oracle при несуществующем пользователе.
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("password-not-set");

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IStorageService _storage;

    public AuthService(AppDbContext db, IConfiguration config, IStorageService storage)
    {
        _db = db;
        _config = config;
        _storage = storage;
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> RegisterAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User"
        };

        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException(
                "Пользователь с таким email или именем уже существует.",
                ex);
        }

        var token = GenerateJwtToken(user);
        return new AuthResponseDto(token, MapToDto(user));
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);

        // Сверяем хэш даже для несуществующего пользователя (фиксированная заглушка),
        // чтобы обе ветки занимали сопоставимое время и не утекал timing-oracle.
        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user is null || !passwordValid)
            throw new UnauthorizedAccessException("Неверный email или пароль.");

        if (user.IsBanned)
            throw new UnauthorizedAccessException("Аккаунт заблокирован.");

        var token = GenerateJwtToken(user);
        return new AuthResponseDto(token, MapToDto(user));
    }

    /// <inheritdoc />
    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto(u.Id, u.Username, u.Email, u.Role, u.AvatarUrl, u.AvatarThumbUrl, u.Bio, u.CreatedAt))
            .FirstOrDefaultAsync(ct);

        // Ключи аватара → абсолютные URL (в памяти).
        return profile is null ? null : profile with
        {
            AvatarUrl = profile.AvatarUrl == null ? null : _storage.ResolveUrl(profile.AvatarUrl),
            AvatarThumbUrl = profile.AvatarThumbUrl == null ? null : _storage.ResolveUrl(profile.AvatarThumbUrl),
        };
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден.");

        user.Username = dto.Username.Trim();
        user.Email = dto.Email.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();
        user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new InvalidOperationException("Пользователь с таким email или именем уже существует.", ex);
        }

        var token = GenerateJwtToken(user);
        return new AuthResponseDto(token, MapToDto(user));
    }

    /// <inheritdoc />
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Текущий пароль неверен.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        // Инвалидируем все ранее выданные токены пользователя.
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);
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
            new Claim(SecurityStampClaimType, user.SecurityStamp),
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

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
