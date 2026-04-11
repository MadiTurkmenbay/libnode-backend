namespace LibNode.Api.Models.DTOs;

public class BookCollectionStatusDto
{
    public Guid CollectionId { get; set; }
    public required string CollectionName { get; set; }
}
