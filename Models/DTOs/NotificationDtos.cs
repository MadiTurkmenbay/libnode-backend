using System.ComponentModel.DataAnnotations;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string? Message,
    string? LinkUrl,
    bool IsRead,
    DateTime CreatedAt
);

public record CreateReportDto(
    [Required, StringLength(500, MinimumLength = 3)]
    string Reason
);

public record CommentReportDto(
    Guid Id,
    Guid CommentId,
    Guid BookId,
    Guid? ChapterId,
    string CommentContent,
    string CommentAuthor,
    string ReporterUsername,
    string Reason,
    bool IsResolved,
    DateTime CreatedAt
);

public record CreateInviteDto(
    [Required]
    string Username,
    [Required]
    TeamRole Role
);

public record TeamInviteDto(
    Guid Id,
    Guid TeamId,
    string TeamName,
    TeamRole Role,
    RequestStatus Status,
    DateTime CreatedAt
);
