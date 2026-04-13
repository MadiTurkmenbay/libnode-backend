using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для сохранения последней открытой главы.
/// </summary>
public record SetProgressDto(
    [Required]
    Guid ChapterId
);
