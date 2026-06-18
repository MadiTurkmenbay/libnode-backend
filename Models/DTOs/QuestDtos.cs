namespace LibNode.Api.Models.DTOs;

/// <summary>Ежедневный квест со статусом прогресса на сегодня.</summary>
public record QuestDto(
    string Key,
    string Title,
    string Icon,
    int Target,
    int Progress,
    int Reward,
    bool Completed
);
