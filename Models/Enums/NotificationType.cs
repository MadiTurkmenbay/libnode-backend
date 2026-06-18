namespace LibNode.Api.Models.Enums;

/// <summary>Тип уведомления пользователя.</summary>
public enum NotificationType
{
    CommentReply = 1,
    TeamInvite = 2,
    RequestApproved = 3,
    RequestRejected = 4,
    NewChapter = 5,
    Mention = 6,
    LevelUp = 7,
    Achievement = 8,
}
