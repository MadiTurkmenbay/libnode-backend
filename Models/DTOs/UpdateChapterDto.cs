using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>DTO редактирования главы (заголовок, текст, номер).</summary>
public record UpdateChapterDto(
    [Required, StringLength(500, MinimumLength = 1)]
    string Title,

    [Required, MinLength(1)]
    string Content,

    [Range(1, int.MaxValue)]
    int ChapterNumber,

    bool IsPublished = true
);
