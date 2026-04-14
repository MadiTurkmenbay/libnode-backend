using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

public record ReaderChapterPayloadDto(
    [Required] Guid ReaderTitleId,

    [Range(1, int.MaxValue)]
    int ChapterNumber,

    [Required, StringLength(500, MinimumLength = 1)]
    string Title,

    [Required, MinLength(1)]
    string Body
);
