using System.ComponentModel.DataAnnotations;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

public record TeamDto(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    int MemberCount,
    int BookCount,
    DateTime CreatedAt,
    bool IsVerified = false
);

public record TeamMemberDto(
    Guid UserId,
    string Username,
    TeamRole Role,
    DateTime CreatedAt
);

public record TeamBookDto(
    Guid Id,
    string Title,
    string? CoverUrl,
    int ChapterCount
);

public record TeamDetailDto(
    Guid Id,
    string Name,
    string? Slug,
    string? Description,
    DateTime CreatedAt,
    IReadOnlyList<TeamMemberDto> Members,
    IReadOnlyList<TeamBookDto> Books,
    TeamRole? MyRole,
    bool IsVerified = false
);

public record CreateTeamDto(
    [Required, StringLength(150, MinimumLength = 2)]
    string Name,
    [StringLength(150)]
    string? Slug,
    [StringLength(2000)]
    string? Description
);

public record UpdateTeamDto(
    [Required, StringLength(150, MinimumLength = 2)]
    string Name,
    [StringLength(2000)]
    string? Description
);

public record AddTeamMemberDto(
    [Required]
    string Username,
    [Required]
    TeamRole Role
);

public record UpdateTeamMemberDto(
    [Required]
    TeamRole Role
);

public record CreateTitleRequestDto(
    [Required]
    Guid BookId,
    [StringLength(1000)]
    string? Message
);

public record TeamTitleRequestDto(
    Guid Id,
    Guid TeamId,
    string TeamName,
    Guid BookId,
    string BookTitle,
    RequestStatus Status,
    string? Message,
    DateTime CreatedAt,
    DateTime? DecidedAt
);
