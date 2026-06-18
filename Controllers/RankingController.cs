using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
public class RankingController : ControllerBase
{
    private readonly IRankingService _rankings;

    public RankingController(IRankingService rankings)
    {
        _rankings = rankings;
    }

    /// <summary>Топ книг по типу рейтинга (публично).</summary>
    [HttpGet("api/rankings")]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> Get([FromQuery] RankingType type, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _rankings.GetRankingAsync(type, limit, ct));
}
