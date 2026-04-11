namespace LibNode.Api.Models.Entities;

/// <summary>
/// Коллекция (пользовательский список) книг.
/// </summary>
public class UserCollection
{
    public Guid Id { get; set; }
    
    /// <summary>ID пользователя, создавшего коллекцию.</summary>
    public Guid UserId { get; set; }
    
    /// <summary>Название коллекции.</summary>
    public required string Name { get; set; }
    
    public DateTime CreatedAt { get; set; }

    /// <summary>Владелец коллекции.</summary>
    public User? User { get; set; }
    
    /// <summary>Книги в этой коллекции.</summary>
    public ICollection<CollectionBook> CollectionBooks { get; set; } = new List<CollectionBook>();
}
