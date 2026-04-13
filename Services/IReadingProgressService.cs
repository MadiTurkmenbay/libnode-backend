namespace LibNode.Api.Services;

/// <summary>
/// Контракт сервиса для сохранения прогресса чтения.
/// </summary>
public interface IReadingProgressService
{
    Task UpsertProgressAsync(Guid userId, Guid bookId, Guid chapterId, CancellationToken ct = default);
}
