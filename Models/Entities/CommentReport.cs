namespace LibNode.Api.Models.Entities;

/// <summary>
/// Жалоба пользователя на комментарий. Попадает в очередь модерации администратора.
/// </summary>
public class CommentReport
{
    public Guid Id { get; set; }

    public Guid CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    public Guid ReporterUserId { get; set; }
    public User Reporter { get; set; } = null!;

    public required string Reason { get; set; }

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; }
}
