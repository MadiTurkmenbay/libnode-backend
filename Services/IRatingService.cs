using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>Оценки и отзывы на книги.</summary>
public interface IRatingService
{
    /// <summary>Поставить/обновить оценку. Возвращает свежий агрегат. Бросает KeyNotFound, если книги нет.</summary>
    Task<RatingAggregateDto> UpsertAsync(Guid userId, Guid bookId, short value, string? review, CancellationToken ct = default);

    /// <summary>Агрегат оценок книги (+ оценка currentUserId, если задан).</summary>
    Task<RatingAggregateDto> GetAggregateAsync(Guid bookId, Guid? currentUserId, CancellationToken ct = default);

    /// <summary>Список отзывов (только с текстом), cursor по UpdatedAt desc.</summary>
    Task<CursorPagedResult<ReviewDto, DateTime>> ListReviewsAsync(Guid bookId, DateTime? cursor, int limit, CancellationToken ct = default);
}
