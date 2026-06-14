using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для обновления заметки к цитате.
/// </summary>
public record UpdateQuoteDto(
    [StringLength(2000)]
    string? Note
);
