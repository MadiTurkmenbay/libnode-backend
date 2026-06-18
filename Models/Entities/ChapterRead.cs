namespace LibNode.Api.Models.Entities;

/// <summary>
/// Факт прочтения конкретной главы пользователем. В отличие от ReadingProgress
/// (последняя позиция), хранит полный набор прочитанных глав — допускает пропуски
/// и непоследовательное чтение. Составной ключ (UserId, ChapterId) идемпотентен.
/// </summary>
public class ChapterRead
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    /// <summary>FK на книгу (денормализовано для выборки прочитанных глав книги).</summary>
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
