using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class QuoteService : IQuoteService
{
    private readonly AppDbContext _context;

    public QuoteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<QuoteDto> CreateQuoteAsync(Guid userId, CreateQuoteDto dto, CancellationToken ct = default)
    {
        var chapter = await _context.Chapters
            .AsNoTracking()
            .Select(c => new { c.Id, c.BookId, c.Title, c.ChapterNumber })
            .FirstOrDefaultAsync(c => c.Id == dto.ChapterId, ct)
            ?? throw new InvalidOperationException("Chapter not found.");

        var quote = new Quote
        {
            UserId = userId,
            ChapterId = dto.ChapterId,
            BookId = chapter.BookId,
            SelectedText = dto.SelectedText,
            ContextText = dto.ContextText,
            Note = dto.Note
        };

        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync(ct);

        var bookTitle = await _context.Books
            .AsNoTracking()
            .Where(b => b.Id == chapter.BookId)
            .Select(b => b.Title)
            .FirstAsync(ct);

        return MapToDto(quote, bookTitle, chapter.Title, chapter.ChapterNumber);
    }

    public async Task<IEnumerable<QuoteDto>> GetUserQuotesAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuoteDto(
                q.Id,
                q.ChapterId,
                q.BookId,
                q.Book.Title,
                q.Chapter.Title,
                q.Chapter.ChapterNumber,
                q.SelectedText,
                q.ContextText,
                q.Note,
                q.CreatedAt,
                q.UpdatedAt
            ))
            .ToListAsync(ct);
    }

    public async Task<QuoteDto?> GetQuoteByIdAsync(Guid quoteId, Guid userId, CancellationToken ct = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Where(q => q.Id == quoteId && q.UserId == userId)
            .Select(q => new QuoteDto(
                q.Id,
                q.ChapterId,
                q.BookId,
                q.Book.Title,
                q.Chapter.Title,
                q.Chapter.ChapterNumber,
                q.SelectedText,
                q.ContextText,
                q.Note,
                q.CreatedAt,
                q.UpdatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<QuoteDto?> UpdateQuoteAsync(Guid quoteId, Guid userId, UpdateQuoteDto dto, CancellationToken ct = default)
    {
        var quote = await _context.Quotes
            .FirstOrDefaultAsync(q => q.Id == quoteId && q.UserId == userId, ct);

        if (quote == null) return null;

        quote.Note = dto.Note;
        await _context.SaveChangesAsync(ct);

        return await GetQuoteByIdAsync(quoteId, userId, ct);
    }

    public async Task<bool> DeleteQuoteAsync(Guid quoteId, Guid userId, CancellationToken ct = default)
    {
        var quote = await _context.Quotes
            .FirstOrDefaultAsync(q => q.Id == quoteId && q.UserId == userId, ct);

        if (quote == null) return false;

        _context.Quotes.Remove(quote);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<QuoteDto>> GetQuotesByBookAsync(Guid bookId, Guid userId, CancellationToken ct = default)
    {
        return await _context.Quotes
            .AsNoTracking()
            .Where(q => q.BookId == bookId && q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuoteDto(
                q.Id,
                q.ChapterId,
                q.BookId,
                q.Book.Title,
                q.Chapter.Title,
                q.Chapter.ChapterNumber,
                q.SelectedText,
                q.ContextText,
                q.Note,
                q.CreatedAt,
                q.UpdatedAt
            ))
            .ToListAsync(ct);
    }

    private static QuoteDto MapToDto(Quote quote, string bookTitle, string chapterTitle, int chapterNumber)
    {
        return new QuoteDto(
            quote.Id,
            quote.ChapterId,
            quote.BookId,
            bookTitle,
            chapterTitle,
            chapterNumber,
            quote.SelectedText,
            quote.ContextText,
            quote.Note,
            quote.CreatedAt,
            quote.UpdatedAt
        );
    }
}
