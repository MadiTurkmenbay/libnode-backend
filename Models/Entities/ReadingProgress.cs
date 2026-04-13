namespace LibNode.Api.Models.Entities;

/// <summary>
/// Прогресс чтения пользователя по книге. Хранит последнюю открытую главу.
/// </summary>
public class ReadingProgress
{
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public Guid ChapterId { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
    public Chapter Chapter { get; set; } = null!;
}
