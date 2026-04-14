using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

public record ReaderTitlePayloadDto(
    [Required, StringLength(300, MinimumLength = 1)]
    string Title,

    [Required, StringLength(300, MinimumLength = 1)]
    string Slug,

    [StringLength(5000)]
    string? Description,

    [Required, StringLength(16, MinimumLength = 2)]
    string Language
);
