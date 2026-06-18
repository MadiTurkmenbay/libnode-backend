using System.Security.Claims;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LibNode.Api.Controllers;

[ApiController]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;

    public CommentsController(ICommentService comments)
    {
        _comments = comments;
    }

    private Guid? GetUserIdOrNull()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid GetUserId()
        => GetUserIdOrNull() ?? throw new UnauthorizedAccessException("User ID claim not found.");

    // ── Book (title) comments ──────────────────────────────────────────

    [HttpGet("api/books/{bookId:guid}/comments")]
    public async Task<ActionResult<CursorPagedResult<CommentDto, Guid>>> GetBookComments(
        Guid bookId, [FromQuery] Guid? cursor, [FromQuery] int limit, [FromQuery] string sort = "new", CancellationToken ct = default)
        => Ok(await _comments.GetBookCommentsAsync(bookId, GetUserIdOrNull(), cursor, limit, sort, ct));

    [HttpPost("api/books/{bookId:guid}/comments")]
    [Authorize]
    [EnableRateLimiting("comments")]
    public async Task<ActionResult<CommentDto>> CreateBookComment(
        Guid bookId, CreateCommentDto dto, CancellationToken ct)
    {
        try
        {
            var comment = await _comments.CreateBookCommentAsync(bookId, GetUserId(), dto.Content, dto.ParentId, ct);
            return CreatedAtAction(nameof(GetBookComments), new { bookId }, comment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Chapter comments ───────────────────────────────────────────────

    [HttpGet("api/chapters/{chapterId:guid}/comments")]
    public async Task<ActionResult<CursorPagedResult<CommentDto, Guid>>> GetChapterComments(
        Guid chapterId, [FromQuery] Guid? cursor, [FromQuery] int limit, [FromQuery] string sort = "new", CancellationToken ct = default)
        => Ok(await _comments.GetChapterCommentsAsync(chapterId, GetUserIdOrNull(), cursor, limit, sort, ct));

    [HttpPost("api/chapters/{chapterId:guid}/comments")]
    [Authorize]
    [EnableRateLimiting("comments")]
    public async Task<ActionResult<CommentDto>> CreateChapterComment(
        Guid chapterId, CreateCommentDto dto, CancellationToken ct)
    {
        try
        {
            var comment = await _comments.CreateChapterCommentAsync(chapterId, GetUserId(), dto.Content, dto.ParentId, ct);
            return CreatedAtAction(nameof(GetChapterComments), new { chapterId }, comment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Votes / pin / deletion ─────────────────────────────────────────

    [HttpPost("api/comments/{id:guid}/vote")]
    [Authorize]
    [EnableRateLimiting("interactions")]
    public async Task<ActionResult<CommentVoteResultDto>> Vote(Guid id, VoteCommentDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _comments.VoteAsync(id, GetUserId(), dto.Value, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("api/comments/{id:guid}/pin")]
    [Authorize]
    public async Task<IActionResult> Pin(Guid id, CancellationToken ct)
    {
        try
        {
            // Закрепляет администратор или глава команды, закреплённой за тайтлом.
            var isPinned = await _comments.SetPinnedAsync(id, GetUserId(), User.IsInRole("Admin"), ct);
            return Ok(new { isPinned });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("api/comments/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _comments.DeleteAsync(id, GetUserId(), User.IsInRole("Admin"), ct);
            return deleted ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("api/comments/{id:guid}/report")]
    [Authorize]
    [EnableRateLimiting("comments")]
    public async Task<IActionResult> Report(Guid id, CreateReportDto dto, CancellationToken ct)
    {
        try
        {
            await _comments.ReportAsync(id, GetUserId(), dto.Reason, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── Admin moderation ───────────────────────────────────────────────

    [HttpGet("api/admin/comments")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CursorPagedResult<CommentDto, Guid>>> GetForModeration(
        [FromQuery] Guid? cursor, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _comments.GetRecentForModerationAsync(cursor, limit, ct));

    [HttpGet("api/admin/comment-reports")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CursorPagedResult<CommentReportDto, Guid>>> GetReports(
        [FromQuery] bool? resolved, [FromQuery] Guid? cursor, [FromQuery] int limit, CancellationToken ct)
        => Ok(await _comments.GetReportsAsync(resolved, cursor, limit, ct));

    [HttpPost("api/admin/comment-reports/{id:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResolveReport(Guid id, CancellationToken ct)
        => await _comments.ResolveReportAsync(id, ct) ? NoContent() : NotFound();
}
