using System.ComponentModel.DataAnnotations;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для создания новой книги. Валидируется на уровне контроллера.
/// </summary>
public record CreateBookDto(
    [Required, StringLength(300, MinimumLength = 1)]
    string Title,

    [StringLength(5000)]
    string? Description,

    [Url]
    string? CoverUrl,

    BookType Type = BookType.Japan,

    OriginalStatus OriginalStatus = OriginalStatus.None,

    TranslationStatus TranslationStatus = TranslationStatus.None,

    List<Guid>? TagIds = null,

    List<Guid>? CategoryIds = null
);
