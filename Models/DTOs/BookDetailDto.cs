using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// Детальный DTO книги для страницы произведения.
/// </summary>
public record BookDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverUrl,
    BookType Type,
    OriginalStatus OriginalStatus,
    TranslationStatus TranslationStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ChapterCount,
    ReadingProgressDto? UserProgress,
    List<TagDto> Tags,
    List<CategoryDto> Categories
);
