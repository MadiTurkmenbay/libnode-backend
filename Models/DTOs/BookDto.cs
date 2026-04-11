namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для отдачи книги клиенту. Не содержит навигационных свойств EF.
/// </summary>
public record BookDto(
    Guid Id,
    string Title,
    string? Description,
    string? CoverUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ChapterCount
);
