using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>Геймификация: начисление XP, уровни, серии чтения.</summary>
public interface IGamificationService
{
    /// <summary>Начислить XP за прочитанную главу (+обновить серию). Вызывать только при НОВОЙ записи ChapterRead.</summary>
    Task AwardChapterReadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Начислить XP за оставленный комментарий.</summary>
    Task AwardCommentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Текущая статистика пользователя (создаёт пустую запись лениво в памяти, без сохранения).</summary>
    Task<UserStatsDto> GetStatsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Сколько суммарного XP нужно, чтобы достичь уровня L (L≥1). reach(1)=0.</summary>
    static int XpToReachLevel(int level)
    {
        double cumulative = 0;
        for (var k = 1; k < level; k++) cumulative += 50 * Math.Pow(k, 1.5);
        return (int)cumulative;
    }

    /// <summary>Уровень для заданного XP по кривой 50·level^1.5 за переход.</summary>
    static int LevelForXp(int xp)
    {
        var level = 1;
        double cumulative = 0;
        while (level < 999)
        {
            var cost = 50 * Math.Pow(level, 1.5);
            if (cumulative + cost > xp) break;
            cumulative += cost;
            level++;
        }
        return level;
    }
}
