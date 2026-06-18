namespace LibNode.Api.Models.DTOs;

/// <summary>Игровая статистика пользователя для клиента.</summary>
public record UserStatsDto(
    int Xp,
    int Level,
    int XpIntoLevel,
    int XpForNextLevel,
    int ChaptersRead,
    int CommentsPosted,
    int CurrentStreak,
    int LongestStreak
);
