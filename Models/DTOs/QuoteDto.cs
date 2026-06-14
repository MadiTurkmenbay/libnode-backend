namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для отдачи цитаты клиенту.
/// </summary>
public record QuoteDto(
    Guid Id,
    Guid ChapterId,
    Guid BookId,
    string BookTitle,
    string ChapterTitle,
    int ChapterNumber,
    string SelectedText,
    string? ContextText,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
