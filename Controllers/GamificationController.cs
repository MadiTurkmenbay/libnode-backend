using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamification;
    private readonly IAchievementService _achievements;
    private readonly IQuestService _quests;

    public GamificationController(IGamificationService gamification, IAchievementService achievements, IQuestService quests)
    {
        _gamification = gamification;
        _achievements = achievements;
        _quests = quests;
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>Игровая статистика текущего пользователя.</summary>
    [HttpGet("api/me/stats")]
    [Authorize]
    public async Task<ActionResult<UserStatsDto>> MyStats(CancellationToken ct)
        => Ok(await _gamification.GetStatsAsync(GetUserId(), ct));

    /// <summary>Каталог достижений со статусом разблокировки.</summary>
    [HttpGet("api/me/achievements")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<AchievementDto>>> MyAchievements(CancellationToken ct)
        => Ok(await _achievements.GetForUserAsync(GetUserId(), ct));

    /// <summary>Ежедневные квесты с прогрессом на сегодня.</summary>
    [HttpGet("api/me/quests")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<QuestDto>>> MyQuests(CancellationToken ct)
        => Ok(await _quests.GetForUserAsync(GetUserId(), ct));
}
