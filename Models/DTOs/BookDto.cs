using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для отдачи книги клиенту. Не содержит навигационных свойств EF.
/// </summary>
public record BookDto(
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
