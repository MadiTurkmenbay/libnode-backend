namespace LibNode.Api.Models.Entities;

/// <summary>
/// Пользовательская цитата (выделенный текст) из главы книги.
/// </summary>
public class Quote
{
    public Guid Id { get; set; }

    /// <summary>FK на пользователя, сохранившего цитату.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK на главу, из которой взята цитата.</summary>
    public Guid ChapterId { get; set; }

    /// <summary>FK на книгу (денормализовано для быстрой группировки по книгам).</summary>
    public Guid BookId { get; set; }

    /// <summary>Выделенный текст.</summary>
    public required string SelectedText { get; set; }

    /// <summary>Окружающий контекст (абзац или предложение).</summary>
    public string? ContextText { get; set; }

    /// <summary>Опциональная заметка пользователя.</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────
    public User User { get; set; } = null!;
    public Chapter Chapter { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
