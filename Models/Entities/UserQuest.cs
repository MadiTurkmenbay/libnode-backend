namespace LibNode.Api.Models.Entities;

/// <summary>
/// Прогресс пользователя по ежедневному квесту за конкретный UTC-день.
/// Каталог квестов задаётся в коде (QuestService). Составной ключ (UserId, QuestKey, Day).
/// </summary>
public class UserQuest
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Ключ квеста из кодового каталога (например "read-1").</summary>
    public string QuestKey { get; set; } = string.Empty;

    /// <summary>UTC-день, к которому относится прогресс.</summary>
    public DateOnly Day { get; set; }

    public int Progress { get; set; }

    /// <summary>Награда уже выдана (чтобы не начислять бонус повторно).</summary>
    public bool Rewarded { get; set; }

    public DateTime CreatedAt { get; set; }
}
