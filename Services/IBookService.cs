using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>
/// Контракт сервиса для работы с книгами.
/// </summary>
public interface IBookService
{
    /// <summary>Получить список книг с курсорной пагинацией (новые → старые).</summary>
    Task<CursorPagedResult<BookDto, Guid>> GetAllAsync(Guid? cursor, int limit = 20, CancellationToken ct = default);

    /// <summary>Получить книгу по ID. Возвращает null, если не найдена.</summary>
    Task<BookDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Создать новую книгу и вернуть её DTO.</summary>
    Task<BookDto> CreateAsync(CreateBookDto dto, CancellationToken ct = default);
}
