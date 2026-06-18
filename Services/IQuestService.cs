using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>Ежедневные квесты (геймификация). Каталог в коде, прогресс — в БД.</summary>
public interface IQuestService
{
    /// <summary>Отметить прочтение главы в дневных квестах. Возвращает суммарный бонус XP за только что выполненные квесты.</summary>
    Task<int> RecordChapterReadAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Отметить комментарий в дневных квестах. Возвращает бонус XP за выполненные квесты.</summary>
    Task<int> RecordCommentAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Квесты пользователя на сегодня (UTC) с прогрессом.</summary>
    Task<IReadOnlyList<QuestDto>> GetForUserAsync(Guid userId, CancellationToken ct = default);
}
