using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
public class RecommendationController : ControllerBase
{
    private readonly IRecommendationService _recs;

    public RecommendationController(IRecommendationService recs)
    {
        _recs = recs;
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>Похожие книги (публично).</summary>
    [HttpGet("api/books/{bookId:guid}/similar")]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> Similar(Guid bookId, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _recs.GetSimilarAsync(bookId, limit, ct));

    /// <summary>Персональные рекомендации (по истории чтения).</summary>
    [HttpGet("api/recommendations")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<BookDto>>> Mine([FromQuery] int limit, CancellationToken ct)
        => Ok(await _recs.GetForUserAsync(GetUserId(), limit, ct));
}
