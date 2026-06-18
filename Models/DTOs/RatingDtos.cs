using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>Поставить/обновить оценку книги.</summary>
public record RateBookDto(
    [Range(1, 5)] short Value,
    [StringLength(2000)] string? Review
);

/// <summary>Агрегат оценок книги + оценка текущего пользователя.</summary>
public record RatingAggregateDto(
    double? Average,
    int Count,
    /// <summary>Распределение по звёздам: [0]=1★ … [4]=5★.</summary>
    int[] Distribution,
    short? MyValue,
    string? MyReview
);

/// <summary>Отзыв пользователя (для списка на странице книги).</summary>
public record ReviewDto(
    Guid UserId,
    string Username,
    string? AvatarThumbUrl,
    short Value,
    string? Review,
    DateTime UpdatedAt
);
