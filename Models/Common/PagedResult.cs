namespace LibNode.Api.Models.Common;

/// <summary>
/// Обобщенный класс для возврата постраничных результатов.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
);
