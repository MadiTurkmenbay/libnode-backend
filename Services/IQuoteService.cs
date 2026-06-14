using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface IQuoteService
{
    Task<QuoteDto> CreateQuoteAsync(Guid userId, CreateQuoteDto dto, CancellationToken ct = default);
    Task<IEnumerable<QuoteDto>> GetUserQuotesAsync(Guid userId, CancellationToken ct = default);
    Task<QuoteDto?> GetQuoteByIdAsync(Guid quoteId, Guid userId, CancellationToken ct = default);
    Task<QuoteDto?> UpdateQuoteAsync(Guid quoteId, Guid userId, UpdateQuoteDto dto, CancellationToken ct = default);
    Task<bool> DeleteQuoteAsync(Guid quoteId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<QuoteDto>> GetQuotesByBookAsync(Guid bookId, Guid userId, CancellationToken ct = default);
}
