using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

public record CreateCategoryDto(
    [Required, StringLength(100, MinimumLength = 1)]
    string Name,

    [Required, StringLength(100, MinimumLength = 1)]
    string Slug
);
