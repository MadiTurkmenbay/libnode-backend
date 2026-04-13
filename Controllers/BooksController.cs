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

    public BooksController(IBookService bookService, IReadingProgressService readingProgressService)
    {
        _bookService = bookService;
        _readingProgressService = readingProgressService;
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
    /// </summary>
    /// <param name="cursor">ID последнего элемента из предыдущей страницы (null для первой страницы).</param>
    /// <param name="limit">Количество элементов на страницу (1-100, по умолчанию 20).</param>
    /// <response code="200">Список книг.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CursorPagedResult<BookDto, Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? cursor = null,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        if (limit < 1) limit = 20;
        if (limit > 100) limit = 100;

        var books = await _bookService.GetAllAsync(cursor, limit, TryGetCurrentUserId(), ct);
        return Ok(books);
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
}
