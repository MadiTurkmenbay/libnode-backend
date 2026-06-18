using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

/// <summary>Запрос на добавление/смену полки.</summary>
public record SetShelfDto(ShelfStatus Status);

/// <summary>Элемент полки: статус + книга.</summary>
public record ShelfItemDto(
    ShelfStatus Status,
    DateTime UpdatedAt,
    BookDto Book
);
