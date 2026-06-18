using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

/// <summary>Запрос на подписку/отписку.</summary>
public record FollowRequestDto(
    FollowTargetType TargetType,
    Guid TargetId
);

/// <summary>Статус подписки на цель + число подписчиков.</summary>
public record FollowStatusDto(
    bool IsFollowing,
    int FollowerCount
);

/// <summary>Элемент ленты активности (новая глава у книги отслеживаемой команды).</summary>
public record FeedItemDto(
    Guid ChapterId,
    Guid BookId,
    string BookTitle,
    string? CoverThumbUrl,
    int ChapterNumber,
    string ChapterTitle,
    Guid? TeamId,
    string? TeamName,
    DateTime CreatedAt
);
