namespace LibNode.Api.Models.DTOs;

public record AdminRecentSignupDto(
    Guid Id,
    string Username,
    string? AvatarUrl,
    DateTime CreatedAt
);

public record AdminMetricsDto(
    int UserCount,
    int BookCount,
    int ChapterCount,
    int CommentCount,
    int ReportCount,
    int TeamCount,
    int PendingRequests,
    int PendingInvites,
    IReadOnlyList<AdminRecentSignupDto> RecentSignups
);
