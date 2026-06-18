using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Участник команды с ролью. Составной ключ (TeamId, UserId).
/// </summary>
public class TeamMember
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public TeamRole Role { get; set; } = TeamRole.Translator;

    public DateTime CreatedAt { get; set; }
}
