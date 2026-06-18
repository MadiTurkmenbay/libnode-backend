using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
public class ChapterVersionsController : ControllerBase
{
    private readonly IChapterVersionService _versions;
    private readonly ITeamService _teams;

    public ChapterVersionsController(IChapterVersionService versions, ITeamService teams)
    {
        _versions = versions;
        _teams = teams;
    }

    private Guid? GetUserIdOrNull()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid GetUserId() => GetUserIdOrNull() ?? throw new UnauthorizedAccessException();

    [HttpGet("api/chapters/{chapterId:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<ChapterVersionDto>>> List(Guid chapterId, CancellationToken ct)
        => Ok(await _versions.GetVersionsAsync(chapterId, GetUserIdOrNull(), ct));

    [HttpGet("api/chapter-versions/{id:guid}")]
    public async Task<ActionResult<ChapterVersionDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var v = await _versions.GetVersionDetailAsync(id, GetUserIdOrNull(), ct);
        return v == null ? NotFound() : Ok(v);
    }

    [HttpPost("api/chapters/{chapterId:guid}/versions")]
    [Authorize]
    public async Task<ActionResult<ChapterVersionDto>> Create(Guid chapterId, CreateChapterVersionDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        // Создавать версию может админ или участник команды тайтла.
        var bookId = await _versions.GetBookIdForChapterAsync(chapterId, ct);
        if (bookId == null) return NotFound();
        if (!await _teams.CanEditBookChaptersAsync(userId, bookId.Value, User.IsInRole("Admin"), ct))
            return Forbid();

        try
        {
            var created = await _versions.CreateAsync(chapterId, userId, dto, ct);
            return Ok(created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("api/chapter-versions/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ChapterVersionDto>> Update(Guid id, UpdateChapterVersionDto dto, CancellationToken ct)
    {
        try
        {
            var updated = await _versions.UpdateAsync(id, GetUserId(), User.IsInRole("Admin"), dto, ct);
            return updated == null ? NotFound() : Ok(updated);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("api/chapter-versions/{id:guid}/vote")]
    [Authorize]
    public async Task<ActionResult<ChapterVersionVoteResultDto>> Vote(Guid id, VoteChapterVersionDto dto, CancellationToken ct)
    {
        try
        {
            return Ok(await _versions.VoteAsync(id, GetUserId(), dto.Value, ct));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("api/chapter-versions/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            return await _versions.DeleteAsync(id, GetUserId(), User.IsInRole("Admin"), ct) ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }
}
