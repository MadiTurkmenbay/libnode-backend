using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// «Полка» пользователя: статус книги (читаю / прочитано / в планах / брошено).
/// Одна запись на пользователя на книгу — составной ключ (UserId, BookId).
/// </summary>
public class BookShelf
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    public ShelfStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
