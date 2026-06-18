namespace LibNode.Api.Models.DTOs;

/// <summary>Одна строка таблицы лидеров (только публичные поля).</summary>
public record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string Username,
    string? AvatarUrl,
    int Level,
    int Value
);

/// <summary>Три таблицы лидеров: по XP, по самой длинной серии, по рейтингу комментариев.</summary>
public record LeaderboardDto(
    IReadOnlyList<LeaderboardEntryDto> TopXp,
    IReadOnlyList<LeaderboardEntryDto> TopStreak,
    IReadOnlyList<LeaderboardEntryDto> TopCommenters
);
