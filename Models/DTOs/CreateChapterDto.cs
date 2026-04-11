using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>
/// DTO для создания новой главы.
/// </summary>
public record CreateChapterDto(
    [Required] Guid BookId,
    
    [Required, StringLength(500, MinimumLength = 1)] 
    string Title,
    
    [Required, MinLength(1)] 
    string Content,
    
    [Range(1, int.MaxValue)]
    int ChapterNumber
);
