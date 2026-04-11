namespace LibNode.Api.Models.Entities;

/// <summary>
/// Сущность книги (ранобэ) в каталоге.
/// </summary>
public class Book
{
    public Guid Id { get; set; }

    /// <summary>Название произведения.</summary>
    public required string Title { get; set; }

    /// <summary>Описание / аннотация.</summary>
    public string? Description { get; set; }

    /// <summary>URL обложки (внешний CDN или локальное хранилище).</summary>
    public string? CoverUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────
    /// <summary>Главы, привязанные к книге (1 : N).</summary>
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    /// <summary>Вхождение книги в коллекции пользователей.</summary>
    public ICollection<CollectionBook> CollectionBooks { get; set; } = new List<CollectionBook>();
}
