using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для входа в систему.
/// </summary>
public record LoginDto(
    [Required, EmailAddress]
    string Email,

    [Required]
    string Password
);
