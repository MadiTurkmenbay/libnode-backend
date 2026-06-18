using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

/// <summary>Личный кабинет: профиль, редактирование, смена пароля.</summary>
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth)
    {
        _auth = auth;
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    [HttpGet("api/me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct)
    {
        var profile = await _auth.GetProfileAsync(GetUserId(), ct);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPut("api/me")]
    public async Task<ActionResult<AuthResponseDto>> Update(UpdateProfileDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.UpdateProfileAsync(GetUserId(), dto, ct));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("api/me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken ct)
    {
        try
        {
            await _auth.ChangePasswordAsync(GetUserId(), dto, ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
