namespace LibNode.Api.Models.Entities;

/// <summary>
/// Голос пользователя за версию перевода главы. Составной ключ (UserId, ChapterVersionId).
/// Value: +1 / -1. Сумма голосов формирует рейтинг версии для выбора лучшей.
/// </summary>
public class ChapterVersionVote
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ChapterVersionId { get; set; }
    public ChapterVersion ChapterVersion { get; set; } = null!;

    public short Value { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
}
