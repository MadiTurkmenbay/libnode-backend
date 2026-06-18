namespace LibNode.Api.Models.Entities;

/// <summary>
/// Голос за комментарий. Составной ключ (UserId, CommentId) обеспечивает идемпотентность.
/// Value: +1 (лайк) или -1 (дизлайк). Итоговый счёт комментария = сумма Value и может быть отрицательным.
/// </summary>
public class CommentLike
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    /// <summary>+1 — лайк, -1 — дизлайк.</summary>
    public short Value { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
}
