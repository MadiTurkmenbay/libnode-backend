namespace LibNode.Api.Models.Common;

/// <summary>
/// Обобщенный класс для возврата результатов курсорной пагинации.
/// </summary>
public record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    Guid? NextCursor,
    bool HasMore
);
