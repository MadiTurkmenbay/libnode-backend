using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>
/// Контракт сервиса аутентификации.
/// </summary>
public interface IAuthService
{
    /// <summary>Зарегистрировать нового пользователя.</summary>
    Task<AuthResponseDto> RegisterAsync(CreateUserDto dto, CancellationToken ct = default);

    /// <summary>Аутентифицировать пользователя и вернуть JWT.</summary>
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);

    /// <summary>Полный профиль пользователя для личного кабинета. Null, если не найден.</summary>
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Обновить профиль (имя/email/аватар/био). Возвращает новый токен (claims могли измениться).</summary>
    Task<AuthResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);

    /// <summary>Сменить пароль (проверяя текущий). Бросает UnauthorizedAccessException при неверном текущем пароле.</summary>
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);

    /// <summary>Обновить ключи аватара и вернуть прежние ключи. Null, если пользователь не найден.</summary>
    Task<(string? AvatarKey, string? AvatarThumbKey)?> UpdateAvatarKeysAsync(
        Guid userId,
        string avatarKey,
        string? avatarThumbKey,
        CancellationToken ct = default);
}
