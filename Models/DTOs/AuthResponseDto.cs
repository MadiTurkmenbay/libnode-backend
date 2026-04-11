namespace LibNode.Api.Models.DTOs;

/// <summary>
/// Ответ на успешную аутентификацию: JWT токен + данные пользователя.
/// </summary>
public record AuthResponseDto(
    string Token,
    UserDto User
);
