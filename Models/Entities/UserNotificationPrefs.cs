namespace LibNode.Api.Models.Entities;

/// <summary>
/// Настройки уведомлений пользователя. По умолчанию все типы включены.
/// Проверяются в NotificationService перед созданием уведомления.
/// </summary>
public class UserNotificationPrefs
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public bool EnableCommentReply { get; set; } = true;
    public bool EnableTeamInvite { get; set; } = true;
    public bool EnableRequestApproved { get; set; } = true;
    public bool EnableRequestRejected { get; set; } = true;
    public bool EnableNewChapter { get; set; } = true;
    public bool EnableMention { get; set; } = true;
    public bool EnableLevelUp { get; set; } = true;
    public bool EnableAchievement { get; set; } = true;

    public DateTime UpdatedAt { get; set; }
}
