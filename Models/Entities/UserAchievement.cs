namespace LibNode.Api.Models.Entities;

/// <summary>
/// Разблокированное пользователем достижение. Каталог достижений задаётся в коде
/// (AchievementService), в БД хранится только факт получения. Составной ключ (UserId, Key).
/// </summary>
public class UserAchievement
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Ключ достижения из кодового каталога (например "first-chapter").</summary>
    public string Key { get; set; } = string.Empty;

    public DateTime UnlockedAt { get; set; }
}
