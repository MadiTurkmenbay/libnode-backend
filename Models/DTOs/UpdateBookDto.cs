using System.ComponentModel.DataAnnotations;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

/// <summary>DTO редактирования метаданных тайтла.</summary>
public record UpdateBookDto(
    [Required, StringLength(300, MinimumLength = 1)]
    string Title,

    [StringLength(5000)]
    string? Description,

    [StringLength(2048)]
    string? CoverUrl,

    BookType Type,
    OriginalStatus OriginalStatus,
    TranslationStatus TranslationStatus
);
