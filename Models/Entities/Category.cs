namespace LibNode.Api.Models.Entities;

public class Category
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
