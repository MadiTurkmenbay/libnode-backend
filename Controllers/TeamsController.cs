using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teams;

    public TeamsController(ITeamService teams)
    {
        _teams = teams;
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private Guid? GetUserIdOrNull()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private bool IsAdmin => User.IsInRole("Admin");

    // ── Public (anonymous) ──────────────────────────────

    [HttpGet("api/public/teams")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> PublicTeams(CancellationToken ct)
        => Ok(await _teams.ListTeamsAsync(ct));

    [HttpGet("api/public/teams/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TeamDetailDto>> PublicTeam(Guid id, CancellationToken ct)
    {
        var team = await _teams.GetTeamAsync(id, GetUserIdOrNull(), ct);
        return team == null ? NotFound() : Ok(team);
    }

    [HttpGet("api/books/{bookId:guid}/team")]
    [AllowAnonymous]
    public async Task<ActionResult<BookTeamDto>> BookTeam(Guid bookId, CancellationToken ct)
    {
        var team = await _teams.GetBookTeamAsync(bookId, ct);
        return team == null ? NoContent() : Ok(team);
    }

    // ── Team (head / member) ────────────────────────────

    [HttpGet("api/teams/mine")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> MyTeams(CancellationToken ct)
        => Ok(await _teams.GetMyTeamsAsync(GetUserId(), ct));

    [HttpGet("api/teams/{id:guid}")]
    public async Task<ActionResult<TeamDetailDto>> GetTeam(Guid id, CancellationToken ct)
    {
        var team = await _teams.GetTeamAsync(id, GetUserId(), ct);
        return team == null ? NotFound() : Ok(team);
    }

    [HttpPost("api/teams/{id:guid}/invites")]
    public async Task<IActionResult> CreateInvite(Guid id, CreateInviteDto dto, CancellationToken ct)
    {
        try
        {
            await _teams.CreateInviteAsync(id, GetUserId(), IsAdmin, dto.Username, dto.Role, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpGet("api/invites/mine")]
    public async Task<ActionResult<IReadOnlyList<TeamInviteDto>>> MyInvites(CancellationToken ct)
        => Ok(await _teams.GetMyInvitesAsync(GetUserId(), ct));

    [HttpPost("api/invites/{id:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid id, CancellationToken ct)
    {
        try
        {
            await _teams.AcceptInviteAsync(id, GetUserId(), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("api/invites/{id:guid}/decline")]
    public async Task<IActionResult> DeclineInvite(Guid id, CancellationToken ct)
    {
        await _teams.DeclineInviteAsync(id, GetUserId(), ct);
        return NoContent();
    }

    [HttpPut("api/teams/{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateMember(Guid id, Guid userId, UpdateTeamMemberDto dto, CancellationToken ct)
    {
        try
        {
            await _teams.UpdateMemberRoleAsync(id, GetUserId(), IsAdmin, userId, dto.Role, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("api/teams/{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        try
        {
            await _teams.RemoveMemberAsync(id, GetUserId(), IsAdmin, userId, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("api/teams/{id:guid}/requests")]
    public async Task<ActionResult<TeamTitleRequestDto>> RequestTitle(Guid id, CreateTitleRequestDto dto, CancellationToken ct)
    {
        try
        {
            var req = await _teams.RequestTitleAsync(id, GetUserId(), IsAdmin, dto.BookId, dto.Message, ct);
            return Ok(req);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── Admin ───────────────────────────────────────────

    [HttpGet("api/admin/teams")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> ListTeams(CancellationToken ct)
        => Ok(await _teams.ListTeamsAsync(ct));

    [HttpPost("api/admin/teams")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeamDto>> CreateTeam(CreateTeamDto dto, CancellationToken ct)
    {
        try
        {
            var team = await _teams.CreateTeamAsync(dto, ct);
            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("api/admin/teams/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeamDto>> UpdateTeam(Guid id, UpdateTeamDto dto, CancellationToken ct)
    {
        var team = await _teams.UpdateTeamAsync(id, dto, ct);
        return team == null ? NotFound() : Ok(team);
    }

    [HttpDelete("api/admin/teams/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTeam(Guid id, CancellationToken ct)
        => await _teams.DeleteTeamAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("api/admin/teams/{id:guid}/verify")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeamDto>> ToggleVerify(Guid id, CancellationToken ct)
    {
        var team = await _teams.ToggleVerifyAsync(id, ct);
        return team == null ? NotFound() : Ok(team);
    }

    [HttpGet("api/admin/team-requests")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<TeamTitleRequestDto>>> ListRequests(
        [FromQuery] RequestStatus? status, CancellationToken ct)
        => Ok(await _teams.ListAllRequestsAsync(status, ct));

    [HttpPost("api/admin/team-requests/{id:guid}/decide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeamTitleRequestDto>> DecideRequest(Guid id, [FromQuery] bool approve, CancellationToken ct)
    {
        var req = await _teams.DecideRequestAsync(id, approve, ct);
        return req == null ? NotFound() : Ok(req);
    }
}
