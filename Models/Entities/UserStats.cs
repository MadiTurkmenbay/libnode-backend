namespace LibNode.Api.Models.Entities;

/// <summary>
/// Игровая статистика пользователя (геймификация): опыт, уровень, серии чтения.
/// Одна запись на пользователя (UserId = PK). Создаётся лениво при первом начислении XP.
/// </summary>
public class UserStats
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Накопленный опыт.</summary>
    public int Xp { get; set; }

    /// <summary>Текущий уровень (производное от Xp, кэшируется для сортировок/отображения).</summary>
    public int Level { get; set; } = 1;

    public int ChaptersRead { get; set; }
    public int CommentsPosted { get; set; }

    /// <summary>Текущая серия дней подряд с ≥1 прочитанной главой.</summary>
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }

    /// <summary>Дата последнего засчитанного дня чтения (для расчёта серии).</summary>
    public DateOnly? LastReadDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
