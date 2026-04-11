using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

public class AddBookToCollectionDto
{
    [Required]
    public Guid BookId { get; set; }
}
