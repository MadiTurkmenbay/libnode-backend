using System.Security.Claims;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;

    public QuotesController(IQuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim == null)
            throw new UnauthorizedAccessException("User ID claim not found.");
        return Guid.Parse(idClaim);
    }

    [HttpPost]
    public async Task<ActionResult<QuoteDto>> CreateQuote(CreateQuoteDto dto, CancellationToken ct)
    {
        try
        {
            var quote = await _quoteService.CreateQuoteAsync(GetUserId(), dto, ct);
            return CreatedAtAction(nameof(GetQuote), new { id = quote.Id }, quote);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuoteDto>>> GetMyQuotes(CancellationToken ct)
    {
        var quotes = await _quoteService.GetUserQuotesAsync(GetUserId(), ct);
        return Ok(quotes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuoteDto>> GetQuote(Guid id, CancellationToken ct)
    {
        var quote = await _quoteService.GetQuoteByIdAsync(id, GetUserId(), ct);
        if (quote == null) return NotFound();
        return Ok(quote);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<QuoteDto>> UpdateQuote(Guid id, UpdateQuoteDto dto, CancellationToken ct)
    {
        var quote = await _quoteService.UpdateQuoteAsync(id, GetUserId(), dto, ct);
        if (quote == null) return NotFound();
        return Ok(quote);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuote(Guid id, CancellationToken ct)
    {
        var deleted = await _quoteService.DeleteQuoteAsync(id, GetUserId(), ct);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("by-book/{bookId}")]
    public async Task<ActionResult<IEnumerable<QuoteDto>>> GetQuotesByBook(Guid bookId, CancellationToken ct)
    {
        var quotes = await _quoteService.GetQuotesByBookAsync(bookId, GetUserId(), ct);
        return Ok(quotes);
    }
}
