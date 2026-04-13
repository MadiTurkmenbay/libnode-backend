using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

/// <summary>
/// Сервис сохранения последней открытой главы пользователя по книге.
/// </summary>
public class ReadingProgressService : IReadingProgressService
{
    private readonly AppDbContext _db;

    public ReadingProgressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task UpsertProgressAsync(Guid userId, Guid bookId, Guid chapterId, CancellationToken ct = default)
    {
        var chapterExists = await _db.Chapters
            .AsNoTracking()
            .AnyAsync(c => c.Id == chapterId && c.BookId == bookId, ct);

        if (!chapterExists)
        {
            throw new ArgumentException("Глава не найдена или не принадлежит указанной книге.");
        }

        var progress = await _db.ReadingProgresses
            .FirstOrDefaultAsync(rp => rp.UserId == userId && rp.BookId == bookId, ct);

        if (progress is null)
        {
            _db.ReadingProgresses.Add(new ReadingProgress
            {
                UserId = userId,
                BookId = bookId,
                ChapterId = chapterId
            });
        }
        else
        {
            progress.ChapterId = chapterId;
            _db.Entry(progress).Property(rp => rp.ChapterId).IsModified = true;
        }

        await _db.SaveChangesAsync(ct);
    }
}
