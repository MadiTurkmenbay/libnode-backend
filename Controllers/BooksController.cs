using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibNode.Api.Controllers;

/// <summary>
/// REST-контроллер каталога книг.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IReadingProgressService _readingProgressService;
    private readonly ITeamService _teams;

    public BooksController(IBookService bookService, IReadingProgressService readingProgressService, ITeamService teams)
    {
        _bookService = bookService;
        _readingProgressService = readingProgressService;
        _teams = teams;
    }

    private Guid? TryGetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Получить список книг с курсорной пагинацией.
    /// Поддерживает сортировки CreatedAt (по умолчанию), UpdatedAt и Title.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CursorStringPagedResult<BookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetBooksQueryDto query,
        CancellationToken ct = default)
    {
        var normalizedLimit = query.Limit;
        if (normalizedLimit < 1) normalizedLimit = 20;
        if (normalizedLimit > 100) normalizedLimit = 100;

        var normalizedQuery = query with { Limit = normalizedLimit };

        try
        {
            var cursorResult = await _bookService.GetAllAsync(normalizedQuery, TryGetCurrentUserId(), ct);
            return Ok(cursorResult);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить книгу по ID.
    /// </summary>
    /// <param name="id">GUID книги.</param>
    /// <response code="200">Книга найдена.</response>
    /// <response code="404">Книга не найдена.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var book = await _bookService.GetByIdAsync(id, TryGetCurrentUserId(), ct);

        if (book is null)
        {
            return NotFound();
        }

        return Ok(book);
    }

    /// <summary>
    /// Создать новую книгу.
    /// </summary>
    /// <param name="dto">Данные для создания книги.</param>
    /// <response code="201">Книга успешно создана.</response>
    /// <response code="400">Невалидные данные.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookDto dto, CancellationToken ct)
    {
        var created = await _bookService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Редактировать метаданные тайтла (администратор или глава команды тайтла).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookDto dto, CancellationToken ct)
    {
        var userId = TryGetCurrentUserId();
        if (userId == null) return Unauthorized();
        if (!await _teams.CanManageBookTitleAsync(userId.Value, id, User.IsInRole("Admin"), ct))
            return Forbid();

        var updated = await _bookService.UpdateAsync(id, dto, ct);
        return updated == null ? NotFound() : Ok(updated);
    }

    /// <summary>
    /// Сохранить последнюю открытую главу пользователя по книге.
    /// </summary>
    [HttpPost("{id:guid}/progress")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetProgress(Guid id, [FromBody] SetProgressDto dto, CancellationToken ct)
    {
        var userId = TryGetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            await _readingProgressService.UpsertProgressAsync(userId.Value, id, dto.ChapterId, ct);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Идентификаторы прочитанных текущим пользователем глав книги (для индикатора «прочитано»).
    /// </summary>
    [HttpGet("{id:guid}/read-chapters")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Guid>>> GetReadChapters(Guid id, CancellationToken ct)
    {
        var userId = TryGetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var ids = await _readingProgressService.GetReadChapterIdsAsync(userId.Value, id, ct);
        return Ok(ids);
    }

    /// <summary>
    /// Отметить прочитанными все опубликованные главы книги до указанного номера включительно.
    /// </summary>
    [HttpPost("{id:guid}/mark-read-through")]
    [Authorize]
    public async Task<IActionResult> MarkReadThrough(Guid id, [FromBody] MarkReadThroughDto dto, CancellationToken ct)
    {
        var userId = TryGetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var added = await _readingProgressService.MarkReadThroughAsync(userId.Value, id, dto.ChapterNumber, ct);
        return Ok(new { added });
    }

    /// <summary>
    /// Блок «Продолжить чтение»: книги пользователя с последней позицией.
    /// </summary>
    [HttpGet("/api/me/continue-reading")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<ContinueReadingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ContinueReadingDto>>> ContinueReading([FromQuery] int limit, CancellationToken ct)
    {
        var userId = TryGetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        var items = await _readingProgressService.GetContinueReadingAsync(userId.Value, limit, ct);
        return Ok(items);
    }
}
