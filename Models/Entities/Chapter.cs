namespace LibNode.Api.Models.Entities;

/// <summary>
/// Глава книги. Содержит текст и порядковый номер.
/// </summary>
public class Chapter
{
    public Guid Id { get; set; }

    /// <summary>FK на книгу.</summary>
    public Guid BookId { get; set; }

    /// <summary>Заголовок главы.</summary>
    public required string Title { get; set; }

    /// <summary>Полный текст главы (HTML / Markdown).</summary>
    public required string Content { get; set; }

    /// <summary>Порядковый номер главы внутри книги.</summary>
    public int ChapterNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────
    /// <summary>Родительская книга.</summary>
    public Book Book { get; set; } = null!;

    /// <summary>Лайки главы.</summary>
    public ICollection<ChapterLike> Likes { get; set; } = new List<ChapterLike>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
}
