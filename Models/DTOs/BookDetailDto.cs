namespace LibNode.Api.Models.DTOs;

/// <summary>
/// Детальный DTO книги для страницы произведения.
/// </summary>
public record BookDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ChapterCount,
    ReadingProgressDto? UserProgress
);
