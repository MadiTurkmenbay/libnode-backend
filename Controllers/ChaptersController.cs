using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibNode.Api.Controllers;

/// <summary>
/// REST-контроллер для управления главами.
/// </summary>
[ApiController]
[Produces("application/json")]
public class ChaptersController : ControllerBase
{
    private readonly IChapterService _chapterService;

    public ChaptersController(IChapterService chapterService)
    {
        _chapterService = chapterService;
    }

    /// <summary>
    /// Получить список глав для конкретной книги с курсорной пагинацией.
    /// </summary>
    /// <param name="bookId">GUID книги.</param>
    /// <param name="cursor">Номер последней главы из предыдущей страницы (null для первой страницы).</param>
    /// <param name="limit">Количество элементов на страницу (1-100, по умолчанию 50).</param>
    /// <param name="sortDesc">true — от новых к старым, false — от старых к новым.</param>
    [HttpGet("api/books/{bookId:guid}/chapters")]
    [ProducesResponseType(typeof(CursorPagedResult<ChapterListDto, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByBookId(
        Guid bookId,
        [FromQuery] int? cursor = null,
        [FromQuery] int limit = 50,
        [FromQuery] bool sortDesc = true,
        CancellationToken ct = default)
    {
        if (limit < 1) limit = 50;
        if (limit > 100) limit = 100;

        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedId))
            {
                userId = parsedId;
            }
        }

        var result = await _chapterService.GetByBookIdAsync(bookId, cursor, limit, sortDesc, userId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить главу по ID (с текстом).
    /// </summary>
    [HttpGet("api/chapters/{id:guid}")]
    [ProducesResponseType(typeof(ChapterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedId))
            {
                userId = parsedId;
            }
        }

        var chapter = await _chapterService.GetByIdAsync(id, userId, ct);

        if (chapter is null)
            return NotFound();

        return Ok(chapter);
    }

    /// <summary>
    /// Создать новую главу.
    /// </summary>
    [HttpPost("api/chapters")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ChapterDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateChapterDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _chapterService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Лайкнуть главу.
    /// </summary>
    [HttpPost("api/chapters/{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Like(Guid id, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            await _chapterService.LikeChapterAsync(id, userId, ct);
            return Ok(new { success = true });
        }
        catch (ArgumentException)
        {
            return NotFound(new { error = "Глава не найдена" });
        }
    }
}
