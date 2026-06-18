using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>Полный профиль текущего пользователя (личный кабинет).</summary>
public record UserProfileDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string? AvatarUrl,
    string? AvatarThumbUrl,
    string? Bio,
    DateTime CreatedAt
);

/// <summary>Редактирование профиля (имя, email, аватар, «о себе»).</summary>
public record UpdateProfileDto(
    [Required, StringLength(50, MinimumLength = 2)]
    string Username,
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [StringLength(500)]
    string? AvatarUrl,
    [StringLength(1000)]
    string? Bio
);

/// <summary>Смена пароля: текущий + новый.</summary>
public record ChangePasswordDto(
    [Required]
    string CurrentPassword,
    [Required, StringLength(100, MinimumLength = 6)]
    string NewPassword
);
