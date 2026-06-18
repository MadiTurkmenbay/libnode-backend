using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>
/// Контракт сервиса для сохранения прогресса чтения.
/// </summary>
public interface IReadingProgressService
{
    Task UpsertProgressAsync(Guid userId, Guid bookId, Guid chapterId, CancellationToken ct = default);

    /// <summary>Идентификаторы всех прочитанных пользователем глав книги (с пропусками).</summary>
    Task<IReadOnlyList<Guid>> GetReadChapterIdsAsync(Guid userId, Guid bookId, CancellationToken ct = default);

    /// <summary>Отметить прочитанными все опубликованные главы книги с номером ≤ throughChapterNumber (идемпотентно). Возвращает число новых отметок.</summary>
    Task<int> MarkReadThroughAsync(Guid userId, Guid bookId, int throughChapterNumber, CancellationToken ct = default);

    /// <summary>Список «Продолжить чтение»: книги с последней позицией, новейшие сверху.</summary>
    Task<IReadOnlyList<ContinueReadingDto>> GetContinueReadingAsync(Guid userId, int limit, CancellationToken ct = default);
}
