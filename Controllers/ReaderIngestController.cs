using LibNode.Api.Authentication;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/reader")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = TranslatorApiKeyAuthenticationDefaults.SchemeName)]
public class ReaderIngestController : ControllerBase
{
    private readonly IReaderIngestService _readerIngestService;

    public ReaderIngestController(IReaderIngestService readerIngestService)
    {
        _readerIngestService = readerIngestService;
    }

    [HttpPost("titles")]
    [ProducesResponseType(typeof(ReaderResourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReaderResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTitle([FromBody] ReaderTitlePayloadDto dto, CancellationToken ct)
    {
        try
        {
            var (resource, created) = await _readerIngestService.CreateOrGetTitleAsync(dto, ct);

            if (created)
            {
                return CreatedAtAction(nameof(BooksController.GetById), "Books", new { id = resource.Id }, resource);
            }

            return Ok(resource);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("chapters")]
    [ProducesResponseType(typeof(ReaderResourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReaderResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChapter([FromBody] ReaderChapterPayloadDto dto, CancellationToken ct)
    {
        try
        {
            var (resource, created) = await _readerIngestService.CreateOrUpdateChapterAsync(dto, ct);

            if (created)
            {
                return CreatedAtAction(nameof(ChaptersController.GetById), "Chapters", new { id = resource.Id }, resource);
            }

            return Ok(resource);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
