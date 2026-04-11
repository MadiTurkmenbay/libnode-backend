namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для списка глав. Не содержит полного текста (Content).
/// </summary>
public record ChapterListDto(
    Guid Id,
    Guid BookId,
    string Title,
    int ChapterNumber,
    DateTime CreatedAt
);
