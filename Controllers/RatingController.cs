using System.Security.Claims;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibNode.Api.Controllers;

[ApiController]
public class RatingController : ControllerBase
{
    private readonly IRatingService _ratings;

    public RatingController(IRatingService ratings)
    {
        _ratings = ratings;
    }

    private Guid? GetUserIdOrNull()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid GetUserId() => GetUserIdOrNull() ?? throw new UnauthorizedAccessException();

    [HttpPost("api/books/{bookId:guid}/rating")]
    [Authorize]
    [EnableRateLimiting("interactions")]
    public async Task<ActionResult<RatingAggregateDto>> Rate(Guid bookId, RateBookDto dto, CancellationToken ct)
    {
        try { return Ok(await _ratings.UpsertAsync(GetUserId(), bookId, dto.Value, dto.Review, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentOutOfRangeException) { return BadRequest(new { error = "Оценка должна быть от 1 до 5." }); }
    }

    [HttpGet("api/books/{bookId:guid}/rating")]
    public async Task<ActionResult<RatingAggregateDto>> Aggregate(Guid bookId, CancellationToken ct)
        => Ok(await _ratings.GetAggregateAsync(bookId, GetUserIdOrNull(), ct));

    [HttpGet("api/books/{bookId:guid}/reviews")]
    public async Task<ActionResult<CursorPagedResult<ReviewDto, DateTime>>> Reviews(
        Guid bookId, [FromQuery] DateTime? cursor, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _ratings.ListReviewsAsync(bookId, cursor, limit, ct));
}
