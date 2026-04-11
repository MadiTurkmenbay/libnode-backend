using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
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
    /// Получить список всех книг.
    /// </summary>
    /// <response code="200">Список книг.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var books = await _bookService.GetAllAsync(pageNumber, pageSize, ct);
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
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookDto dto, CancellationToken ct)
    {
        var created = await _bookService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
