using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Сущность книги (ранобэ) в каталоге.
/// </summary>
public class Book
{
    public Guid Id { get; set; }

    /// <summary>Название произведения.</summary>
    public required string Title { get; set; }

    public string? Slug { get; set; }

    /// <summary>Описание / аннотация.</summary>
    public string? Description { get; set; }

    /// <summary>URL обложки (внешний CDN или локальное хранилище).</summary>
    public string? CoverUrl { get; set; }

    /// <summary>URL уменьшенной копии обложки (превью для списков).</summary>
    public string? CoverThumbUrl { get; set; }

    /// <summary>Тип / страна происхождения произведения.</summary>
    public BookType Type { get; set; } = BookType.Japan;

    /// <summary>Статус оригинала.</summary>
    public OriginalStatus OriginalStatus { get; set; } = OriginalStatus.None;

    /// <summary>Статус перевода.</summary>
    public TranslationStatus TranslationStatus { get; set; } = TranslationStatus.None;

    /// <summary>Счётчик просмотров тайтла (пока не отображается на фронте).</summary>
    public long ViewCount { get; set; }

    /// <summary>Команда, закреплённая за тайтлом (максимум одна). Null — без команды.</summary>
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────
    /// <summary>Главы, привязанные к книге (1 : N).</summary>
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

    /// <summary>Вхождение книги в коллекции пользователей.</summary>
    public ICollection<CollectionBook> CollectionBooks { get; set; } = new List<CollectionBook>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();

    /// <summary>Теги книги (М : М).</summary>
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    /// <summary>Категории книги (М : М).</summary>
    public ICollection<Category> Categories { get; set; } = new List<Category>();

    /// <summary>Оценки книги (для среднего рейтинга).</summary>
    public ICollection<BookRating> Ratings { get; set; } = new List<BookRating>();
}
