using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

public record AdminUserDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    DateTime CreatedAt,
    bool IsBanned = false,
    bool IsMuted = false
);

public record UpdateUserRoleDto(
    [Required, RegularExpression("^(User|Admin)$", ErrorMessage = "Роль должна быть User или Admin.")]
    string Role
);
