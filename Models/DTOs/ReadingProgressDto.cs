namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO прогресса чтения по книге для текущего пользователя.
/// </summary>
public record ReadingProgressDto(
    Guid ChapterId,
    int ChapterNumber
);
