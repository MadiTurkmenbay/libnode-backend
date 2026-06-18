using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Приглашение пользователя в команду с предложенной ролью. Пользователь принимает
/// или отклоняет. Status переиспользует RequestStatus: Pending/Approved(принято)/Rejected(отклонено).
/// </summary>
public class TeamInvite
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public TeamRole Role { get; set; } = TeamRole.Translator;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
}
