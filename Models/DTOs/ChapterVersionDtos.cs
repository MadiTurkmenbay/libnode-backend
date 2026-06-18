using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>Версия перевода главы для отдачи клиенту (без полного текста в списке).</summary>
public record ChapterVersionDto(
    Guid Id,
    Guid ChapterId,
    Guid? TeamId,
    string? TeamName,
    string Title,
    string Language,
    int Score,
    int MyVote,
    bool IsPublished,
    bool IsOwn,
    DateTime CreatedAt
);

/// <summary>Версия перевода с полным текстом (для чтения).</summary>
public record ChapterVersionDetailDto(
    Guid Id,
    Guid ChapterId,
    Guid? TeamId,
    string? TeamName,
    string Title,
    string Content,
    string Language,
    int Score,
    int MyVote,
    bool IsPublished,
    DateTime CreatedAt
);

public record CreateChapterVersionDto(
    [Required, StringLength(500, MinimumLength = 1)]
    string Title,
    [Required, MinLength(1)]
    string Content,
    [StringLength(10)]
    string? Language
);

public record UpdateChapterVersionDto(
    [Required, StringLength(500, MinimumLength = 1)]
    string Title,
    [Required, MinLength(1)]
    string Content,
    [StringLength(10)]
    string? Language,
    bool IsPublished
);

public record VoteChapterVersionDto(
    [Range(-1, 1)]
    short Value
);

public record ChapterVersionVoteResultDto(
    Guid VersionId,
    int Score,
    int MyVote
);
