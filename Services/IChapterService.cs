using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>
/// Контракт сервиса для работы с главами.
/// </summary>
public interface IChapterService
{
    /// <summary>Получить список глав книги с курсорной пагинацией (без текста).</summary>
    Task<CursorPagedResult<ChapterListDto, int>> GetByBookIdAsync(Guid bookId, int? cursor, int limit = 50, bool sortDesc = true, Guid? userId = null, bool includeUnpublished = false, CancellationToken ct = default);

    /// <summary>Получить конкретную главу (с текстом).</summary>
    Task<ChapterDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, bool includeUnpublished = false, CancellationToken ct = default);

    /// <summary>Добавить новую главу.</summary>
    Task<ChapterDetailDto> CreateAsync(CreateChapterDto dto, CancellationToken ct = default);

    /// <summary>Получить bookId главы (для проверки прав). Null, если глава не найдена.</summary>
    Task<Guid?> GetBookIdAsync(Guid chapterId, CancellationToken ct = default);

    /// <summary>Редактировать главу. Null, если не найдена.</summary>
    Task<ChapterDetailDto?> UpdateAsync(Guid id, UpdateChapterDto dto, CancellationToken ct = default);

    /// <summary>Удалить главу. False, если не найдена.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Лайкнуть главу.</summary>
    Task LikeChapterAsync(Guid chapterId, Guid userId, CancellationToken ct = default);
}
