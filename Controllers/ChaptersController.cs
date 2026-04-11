using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Mvc;

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
    /// Получить список глав для конкретной книги.
    /// </summary>
    [HttpGet("api/books/{bookId:guid}/chapters")]
    [ProducesResponseType(typeof(PagedResult<ChapterListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByBookId(
        Guid bookId, 
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 20, 
        CancellationToken ct = default)
    {
        // Базовая валидация
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100; // ограничение на максимальный размер страницы

        var result = await _chapterService.GetByBookIdAsync(bookId, pageNumber, pageSize, ct);
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
        var chapter = await _chapterService.GetByIdAsync(id, ct);

        if (chapter is null)
            return NotFound();

        return Ok(chapter);
    }

    /// <summary>
    /// Создать новую главу.
    /// </summary>
    [HttpPost("api/chapters")]
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
}
