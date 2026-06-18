using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>Эвристические рекомендации книг.</summary>
public interface IRecommendationService
{
    /// <summary>Похожие книги (по общим тегам/категориям), исключая саму книгу.</summary>
    Task<IReadOnlyList<BookDto>> GetSimilarAsync(Guid bookId, int limit, CancellationToken ct = default);

    /// <summary>Персональные рекомендации по истории чтения (исключая прочитанное). Фолбэк — популярное.</summary>
    Task<IReadOnlyList<BookDto>> GetForUserAsync(Guid userId, int limit, CancellationToken ct = default);
}
