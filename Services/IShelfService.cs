using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Services;

/// <summary>Полки пользователя (статусы чтения книг).</summary>
public interface IShelfService
{
    /// <summary>Поставить/сменить статус книги на полке. Бросает KeyNotFound, если книги нет.</summary>
    Task UpsertAsync(Guid userId, Guid bookId, ShelfStatus status, CancellationToken ct = default);

    /// <summary>Убрать книгу с полок. Возвращает true, если запись была.</summary>
    Task<bool> RemoveAsync(Guid userId, Guid bookId, CancellationToken ct = default);

    /// <summary>Текущий статус книги для пользователя (null, если не на полке).</summary>
    Task<ShelfStatus?> GetStatusAsync(Guid userId, Guid bookId, CancellationToken ct = default);

    /// <summary>Список полок пользователя (опционально по статусу), новейшие сверху.</summary>
    Task<IReadOnlyList<ShelfItemDto>> GetShelvesAsync(Guid userId, ShelfStatus? status, CancellationToken ct = default);
}
