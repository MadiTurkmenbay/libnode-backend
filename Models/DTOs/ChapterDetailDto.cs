namespace LibNode.Api.Models.DTOs;

/// <summary>
/// Полный DTO главы, включая её содержимое.
/// </summary>
public record ChapterDetailDto(
    Guid Id,
    Guid BookId,
    string Title,
    string Content,
    int ChapterNumber,
    DateTime CreatedAt
);
