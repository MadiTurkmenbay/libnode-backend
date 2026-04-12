using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
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

        var books = await _bookService.GetAllAsync(cursor, limit, ct);
        return Ok(books);
    }

    /// <summary>
    /// Получить книгу по ID.
    /// </summary>
    /// <param name="id">GUID книги.</param>
    /// <response code="200">Книга найдена.</response>
    /// <response code="404">Книга не найдена.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var book = await _bookService.GetByIdAsync(id, ct);

        if (book is null)
            return NotFound();

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
}
