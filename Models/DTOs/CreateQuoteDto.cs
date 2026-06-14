using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для создания новой цитаты.
/// </summary>
public record CreateQuoteDto(
    [Required]
    Guid ChapterId,

    [Required, StringLength(5000, MinimumLength = 1)]
    string SelectedText,

    [StringLength(5000)]
    string? ContextText,

    [StringLength(2000)]
    string? Note
);
