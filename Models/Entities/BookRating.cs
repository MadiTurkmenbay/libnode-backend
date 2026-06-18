namespace LibNode.Api.Models.Entities;

/// <summary>
/// Оценка книги пользователем (1–5) с необязательным текстовым отзывом.
/// Одна оценка на пользователя на книгу — составной ключ (UserId, BookId).
/// </summary>
public class BookRating
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    /// <summary>Оценка 1–5.</summary>
    public short Value { get; set; }

    /// <summary>Необязательный текстовый отзыв.</summary>
    public string? Review { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
