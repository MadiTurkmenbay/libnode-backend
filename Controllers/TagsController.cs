using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TagDto>>> GetAll(CancellationToken ct)
    {
        var tags = await _tagService.GetAllAsync(ct);
        return Ok(tags);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Create([FromBody] CreateTagDto dto, CancellationToken ct)
    {
        var created = await _tagService.CreateAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Update(Guid id, [FromBody] CreateTagDto dto, CancellationToken ct)
    {
        var updated = await _tagService.UpdateAsync(id, dto, ct);

        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _tagService.DeleteAsync(id, ct);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
