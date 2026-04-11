namespace LibNode.Api.Models.DTOs;

public class CollectionDetailDto : CollectionDto
{
    public List<BookDto> Books { get; set; } = new();
}
