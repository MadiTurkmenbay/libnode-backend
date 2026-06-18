using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Authorize]
public class ShelfController : ControllerBase
{
    private readonly IShelfService _shelves;

    public ShelfController(IShelfService shelves)
    {
        _shelves = shelves;
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    [HttpPut("api/books/{bookId:guid}/shelf")]
    public async Task<IActionResult> Set(Guid bookId, SetShelfDto dto, CancellationToken ct)
    {
        try
        {
            await _shelves.UpsertAsync(GetUserId(), bookId, dto.Status, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("api/books/{bookId:guid}/shelf")]
    public async Task<IActionResult> Remove(Guid bookId, CancellationToken ct)
        => await _shelves.RemoveAsync(GetUserId(), bookId, ct) ? NoContent() : NotFound();

    [HttpGet("api/books/{bookId:guid}/shelf")]
    public async Task<ActionResult<object>> Status(Guid bookId, CancellationToken ct)
        => Ok(new { status = await _shelves.GetStatusAsync(GetUserId(), bookId, ct) });

    [HttpGet("api/me/shelves")]
    public async Task<ActionResult<IReadOnlyList<ShelfItemDto>>> Mine([FromQuery] ShelfStatus? status, CancellationToken ct)
        => Ok(await _shelves.GetShelvesAsync(GetUserId(), status, ct));
}
