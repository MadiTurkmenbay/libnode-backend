using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

public class CreateCollectionDto
{
    [Required]
    [MaxLength(150)]
    public required string Name { get; set; }
}
