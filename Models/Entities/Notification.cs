using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Уведомление пользователя (ответ на коммент, инвайт, решение по заявке, новая глава).
/// Текст рендерится при создании; ссылка ведёт на связанную страницу.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Получатель уведомления.</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public required string Title { get; set; }

    public string? Message { get; set; }

    /// <summary>Относительная ссылка на связанную страницу (например, /books/{id}).</summary>
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
