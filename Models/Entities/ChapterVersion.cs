namespace LibNode.Api.Models.Entities;

/// <summary>
/// Версия перевода главы (ветка перевода). У одной главы (слота) может быть несколько
/// версий от разных команд; читатель выбирает версию. Существующий Chapter.Content
/// остаётся каноническим легаси-полем — версии добавлены аддитивно.
/// </summary>
public class ChapterVersion
{
    public Guid Id { get; set; }

    /// <summary>FK на главу-слот.</summary>
    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    /// <summary>Команда-автор версии. Null — версия без команды (легаси/админ).</summary>
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>Автор версии (пользователь). Null — неизвестен/легаси.</summary>
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }

    public required string Title { get; set; }
    public required string Content { get; set; }

    /// <summary>Язык версии (ISO-код), по умолчанию ru.</summary>
    public string Language { get; set; } = "ru";

    public bool IsPublished { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ChapterVersionVote> Votes { get; set; } = new List<ChapterVersionVote>();
}
