namespace LibNode.Api.Models.Common;

/// <summary>
/// Обобщенный класс для возврата результатов курсорной пагинации.
/// T — тип элемента, TCursor — тип курсора (Guid для книг, int для глав и т.д.).
/// </summary>
public record CursorPagedResult<T, TCursor>(
    IReadOnlyList<T> Items,
    TCursor? NextCursor,
    bool HasMore
) where TCursor : struct;
