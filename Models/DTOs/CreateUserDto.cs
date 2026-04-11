using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для регистрации нового пользователя.
/// </summary>
public record CreateUserDto(
    [Required, StringLength(50, MinimumLength = 3)]
    string Username,

    [Required, EmailAddress, StringLength(256)]
    string Email,

    [Required, StringLength(128, MinimumLength = 6)]
    string Password
);
