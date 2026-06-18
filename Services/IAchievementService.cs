using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>Достижения (геймификация). Каталог в коде, разблокировки — в БД.</summary>
public interface IAchievementService
{
    /// <summary>Пересчитать и выдать новые достижения по текущему состоянию пользователя.</summary>
    Task EvaluateAsync(Guid userId, bool nightRead = false, CancellationToken ct = default);

    /// <summary>Каталог достижений со статусом разблокировки для пользователя (с ленивым пересчётом).</summary>
    Task<IReadOnlyList<AchievementDto>> GetForUserAsync(Guid userId, CancellationToken ct = default);
}
