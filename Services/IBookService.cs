using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>
/// Контракт сервиса для работы с книгами.
/// </summary>
public interface IBookService
{
    /// <summary>Получить список книг с курсорной пагинацией для активных режимов сортировки.</summary>
    Task<CursorStringPagedResult<BookDto>> GetAllAsync(GetBooksQueryDto query, Guid? userId = null, CancellationToken ct = default);

    /// <summary>Получить список книг с offset-пагинацией и произвольной сортировкой.</summary>
    Task<PagedResult<BookDto>> GetAllWithOffsetAsync(GetBooksQueryDto query, Guid? userId = null, CancellationToken ct = default);

    /// <summary>Получить книгу по ID. Возвращает null, если не найдена.</summary>
    Task<BookDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken ct = default);

    /// <summary>Создать новую книгу и вернуть её DTO.</summary>
    Task<BookDto> CreateAsync(CreateBookDto dto, CancellationToken ct = default);

    /// <summary>Редактировать метаданные тайтла. Null, если не найден.</summary>
    Task<BookDto?> UpdateAsync(Guid id, UpdateBookDto dto, CancellationToken ct = default);

    /// <summary>Обновить ключи обложки и вернуть прежние ключи. Null, если книга не найдена.</summary>
    Task<(string? CoverKey, string? CoverThumbKey)?> UpdateCoverKeysAsync(
        Guid id,
        string coverKey,
        string? coverThumbKey,
        CancellationToken ct = default);
}
