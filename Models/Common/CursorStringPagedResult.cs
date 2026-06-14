namespace LibNode.Api.Models.Common;

/// <summary>
/// Класс для возврата результатов курсорной пагинации со строковым курсором.
/// Используется для каталога книг, где курсор кодирует значение сортировки и ID.
/// </summary>
public record CursorStringPagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore
);
