using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface IChapterVersionService
{
    Task<IReadOnlyList<ChapterVersionDto>> GetVersionsAsync(Guid chapterId, Guid? currentUserId, CancellationToken ct = default);
    Task<ChapterVersionDetailDto?> GetVersionDetailAsync(Guid versionId, Guid? currentUserId, CancellationToken ct = default);
    Task<ChapterVersionDto> CreateAsync(Guid chapterId, Guid userId, CreateChapterVersionDto dto, CancellationToken ct = default);

    /// <summary>Редактирование версии. Доступно админу, автору версии или участнику её команды. Бросает Unauthorized если нет прав; null если версия не найдена.</summary>
    Task<ChapterVersionDto?> UpdateAsync(Guid versionId, Guid userId, bool isAdmin, UpdateChapterVersionDto dto, CancellationToken ct = default);

    Task<ChapterVersionVoteResultDto> VoteAsync(Guid versionId, Guid userId, short value, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid versionId, Guid userId, bool isAdmin, CancellationToken ct = default);

    /// <summary>bookId главы версии (для проверки прав). Null, если версия/глава не найдена.</summary>
    Task<Guid?> GetBookIdForVersionAsync(Guid versionId, CancellationToken ct = default);

    /// <summary>bookId главы по chapterId (для проверки прав при создании версии).</summary>
    Task<Guid?> GetBookIdForChapterAsync(Guid chapterId, CancellationToken ct = default);
}
