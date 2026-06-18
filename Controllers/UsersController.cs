using System.Security.Claims;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _users;

    public UsersController(IUserAdminService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResult<AdminUserDto, Guid>>> List(
        [FromQuery] string? search, [FromQuery] Guid? cursor, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _users.ListAsync(search, cursor, limit, ct));

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<AdminUserDto>> UpdateRole(Guid id, UpdateUserRoleDto dto, CancellationToken ct)
    {
        var selfId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(selfId, out var sid) && sid == id && dto.Role != "Admin")
            return BadRequest(new { error = "Нельзя разжаловать собственный аккаунт." });

        var updated = await _users.UpdateRoleAsync(id, dto.Role, ct);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpPost("{id:guid}/ban")]
    public async Task<ActionResult<AdminUserDto>> ToggleBan(Guid id, CancellationToken ct)
    {
        var selfId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(selfId, out var sid) && sid == id)
            return BadRequest(new { error = "Нельзя забанить собственный аккаунт." });
        var updated = await _users.ToggleBanAsync(id, ct);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpPost("{id:guid}/mute")]
    public async Task<ActionResult<AdminUserDto>> ToggleMute(Guid id, CancellationToken ct)
    {
        var updated = await _users.ToggleMuteAsync(id, ct);
        return updated == null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var selfId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(selfId, out var sid) && sid == id)
            return BadRequest(new { error = "Нельзя удалить собственный аккаунт." });

        return await _users.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
