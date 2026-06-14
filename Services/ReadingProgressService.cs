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

        await _db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ReadingProgresses\" (\"UserId\", \"BookId\", \"ChapterId\", \"UpdatedAt\") VALUES ({userId}, {bookId}, {chapterId}, now()) ON CONFLICT (\"UserId\", \"BookId\") DO UPDATE SET \"ChapterId\" = EXCLUDED.\"ChapterId\", \"UpdatedAt\" = now()", ct);
    }
}
