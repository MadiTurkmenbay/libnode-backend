using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

/// <summary>
/// Бизнес-логика для работы с главами.
/// </summary>
public class ChapterService : IChapterService
{
    private readonly AppDbContext _db;

    public ChapterService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ChapterListDto>> GetByBookIdAsync(Guid bookId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Chapters
            .AsNoTracking()
            .Where(c => c.BookId == bookId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.ChapterNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ChapterListDto(
                c.Id,
                c.BookId,
                c.Title,
                c.ChapterNumber,
                c.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<ChapterListDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<ChapterDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Chapters
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ChapterDetailDto(
                c.Id,
                c.BookId,
                c.Title,
                c.Content,
                c.ChapterNumber,
                c.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ChapterDetailDto> CreateAsync(CreateChapterDto dto, CancellationToken ct = default)
    {
        // Проверяем существование книги
        var bookExists = await _db.Books.AnyAsync(b => b.Id == dto.BookId, ct);
        if (!bookExists)
            throw new ArgumentException($"Книга с Id {dto.BookId} не найдена.");

        var chapter = new Chapter
        {
            BookId = dto.BookId,
            Title = dto.Title,
            Content = dto.Content,
            ChapterNumber = dto.ChapterNumber,
            CreatedAt = DateTime.UtcNow
        };

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(ct);

        return new ChapterDetailDto(
            chapter.Id,
            chapter.BookId,
            chapter.Title,
            chapter.Content,
            chapter.ChapterNumber,
            chapter.CreatedAt
        );
    }
}
