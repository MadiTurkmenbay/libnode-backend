namespace LibNode.Api.Models.DTOs;

public class CollectionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BookCount { get; set; }
}
