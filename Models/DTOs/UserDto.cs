namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO пользователя (без конфиденциальных данных).
/// </summary>
public record UserDto(
    Guid Id,
    string Username,
    string Email,
    string Role
);
