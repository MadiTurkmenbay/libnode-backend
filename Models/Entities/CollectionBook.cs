namespace LibNode.Api.Models.Entities;

/// <summary>
/// Связующая таблица (Многие-ко-Многим) между UserCollection и Book.
/// </summary>
public class CollectionBook
{
    public Guid CollectionId { get; set; }
    public Guid BookId { get; set; }
    public DateTime AddedAt { get; set; }

    public UserCollection? Collection { get; set; }
    public Book? Book { get; set; }
}
