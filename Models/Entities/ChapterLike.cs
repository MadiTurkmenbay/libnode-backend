using System;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Сущность лайка главы.
/// </summary>
public class ChapterLike
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
