namespace LibNode.Api.Models.Entities;

/// <summary>
/// Комментарий пользователя к книге (тайтлу) или к конкретной главе.
/// ChapterId == null означает комментарий уровня книги; иначе — комментарий главы.
/// </summary>
public class Comment
{
    public Guid Id { get; set; }

    /// <summary>FK на книгу (денормализовано: задаётся и для комментариев глав).</summary>
    public Guid BookId { get; set; }

    /// <summary>FK на главу. Null — комментарий уровня книги/тайтла.</summary>
    public Guid? ChapterId { get; set; }

    /// <summary>FK на автора комментария.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK на родительский комментарий. Null — корневой комментарий; иначе ответ.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Закреплён ли комментарий (только для комментариев тайтла; максимум один на книгу).</summary>
    public bool IsPinned { get; set; }

    /// <summary>Текст комментария.</summary>
    public required string Content { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────
    public Book Book { get; set; } = null!;
    public Chapter? Chapter { get; set; }
    public User User { get; set; } = null!;
    public Comment? Parent { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();
}
