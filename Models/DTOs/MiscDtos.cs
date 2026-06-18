namespace LibNode.Api.Models.DTOs;

/// <summary>Краткая инфо о команде, закреплённой за тайтлом.</summary>
public record BookTeamDto(
    Guid TeamId,
    string TeamName,
    string? Slug
);

/// <summary>Элемент блока «Продолжить чтение».</summary>
public record ContinueReadingDto(
    Guid BookId,
    string BookTitle,
    string? CoverUrl,
    Guid LastChapterId,
    int LastChapterNumber,
    DateTime UpdatedAt
);
