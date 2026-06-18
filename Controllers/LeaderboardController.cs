using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

/// <summary>Публичные таблицы лидеров (геймификация).</summary>
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboard;

    public LeaderboardController(ILeaderboardService leaderboard)
    {
        _leaderboard = leaderboard;
    }

    [HttpGet("api/leaderboard")]
    public async Task<ActionResult<LeaderboardDto>> Get([FromQuery] int limit, CancellationToken ct)
        => Ok(await _leaderboard.GetAsync(limit, ct));
}
