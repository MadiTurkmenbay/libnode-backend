using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;

    public CollectionsController(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim == null)
            throw new UnauthorizedAccessException("User ID claim not found.");
        return Guid.Parse(idClaim);
    }

    [HttpPost]
    public async Task<ActionResult<CollectionDto>> CreateCollection(CreateCollectionDto dto)
    {
        var collection = await _collectionService.CreateCollectionAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CollectionDto>>> GetMyCollections()
    {
        var collections = await _collectionService.GetUserCollectionsAsync(GetUserId());
        return Ok(collections);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionDetailDto>> GetCollection(Guid id)
    {
        var collection = await _collectionService.GetCollectionByIdAsync(id, GetUserId());
        if (collection == null) return NotFound();
        return Ok(collection);
    }

    [HttpGet("containing-book/{bookId}")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetCollectionIdsForBook(Guid bookId)
    {
        var collectionIds = await _collectionService.GetCollectionIdsWithBookAsync(bookId, GetUserId());
        return Ok(collectionIds);
    }

    [HttpGet("/api/books/{bookId}/collection-status")]
    public async Task<ActionResult<BookCollectionStatusDto>> GetBookCollectionStatus(Guid bookId)
    {
        var status = await _collectionService.GetBookCollectionStatusAsync(bookId, GetUserId());
        if (status == null) return NoContent();
        return Ok(status);
    }

    [HttpPost("{id}/books")]
    public async Task<IActionResult> AddBookToCollection(Guid id, [FromBody] AddBookToCollectionDto dto, CancellationToken ct)
    {
        try
        {
            await _collectionService.AddBookToCollectionAsync(id, dto.BookId, GetUserId(), ct);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id}/books/{bookId}")]
    public async Task<IActionResult> RemoveBookFromCollection(Guid id, Guid bookId)
    {
        try
        {
            await _collectionService.RemoveBookFromCollectionAsync(id, bookId, GetUserId());
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
