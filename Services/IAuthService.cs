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
}
