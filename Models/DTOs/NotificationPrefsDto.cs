namespace LibNode.Api.Models.DTOs;

public record NotificationPrefsDto(
    bool EnableCommentReply,
    bool EnableTeamInvite,
    bool EnableRequestApproved,
    bool EnableRequestRejected,
    bool EnableNewChapter,
    bool EnableMention,
    bool EnableLevelUp,
    bool EnableAchievement
);

public record UpdateNotificationPrefsDto(
    bool? EnableCommentReply = null,
    bool? EnableTeamInvite = null,
    bool? EnableRequestApproved = null,
    bool? EnableRequestRejected = null,
    bool? EnableNewChapter = null,
    bool? EnableMention = null,
    bool? EnableLevelUp = null,
    bool? EnableAchievement = null
);
