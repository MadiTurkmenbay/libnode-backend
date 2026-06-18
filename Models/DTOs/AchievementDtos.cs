namespace LibNode.Api.Models.DTOs;

/// <summary>Достижение для клиента: метаданные каталога + статус разблокировки.</summary>
public record AchievementDto(
    string Key,
    string Title,
    string Description,
    string Icon,
    bool Unlocked,
    DateTime? UnlockedAt
);
