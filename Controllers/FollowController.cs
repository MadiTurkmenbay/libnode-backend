using System.Security.Claims;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibNode.Api.Controllers;

[ApiController]
public class FollowController : ControllerBase
{
    private readonly IFollowService _follows;

    public FollowController(IFollowService follows)
    {
        _follows = follows;
    }

    private Guid? GetUserIdOrNull()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid GetUserId() => GetUserIdOrNull() ?? throw new UnauthorizedAccessException();

    [HttpPost("api/follow")]
    [Authorize]
    [EnableRateLimiting("interactions")]
    public async Task<ActionResult<FollowStatusDto>> Follow(FollowRequestDto dto, CancellationToken ct)
    {
        try { return Ok(await _follows.FollowAsync(GetUserId(), dto.TargetType, dto.TargetId, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("api/follow")]
    [Authorize]
    [EnableRateLimiting("interactions")]
    public async Task<ActionResult<FollowStatusDto>> Unfollow(
        [FromQuery] FollowTargetType targetType, [FromQuery] Guid targetId, CancellationToken ct)
        => Ok(await _follows.UnfollowAsync(GetUserId(), targetType, targetId, ct));

    [HttpGet("api/follow/status")]
    public async Task<ActionResult<FollowStatusDto>> Status(
        [FromQuery] FollowTargetType targetType, [FromQuery] Guid targetId, CancellationToken ct)
        => Ok(await _follows.GetStatusAsync(GetUserIdOrNull(), targetType, targetId, ct));

    [HttpGet("api/feed")]
    [Authorize]
    public async Task<ActionResult<CursorPagedResult<FeedItemDto, Guid>>> Feed(
        [FromQuery] Guid? cursor, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _follows.GetFeedAsync(GetUserId(), cursor, limit, ct));
}
