using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

/// <summary>
/// Сервис сохранения последней открытой главы пользователя по книге.
/// </summary>
public class ReadingProgressService : IReadingProgressService
{
    private readonly AppDbContext _db;
    private readonly IGamificationService _gamification;

    public ReadingProgressService(AppDbContext db, IGamificationService gamification)
    {
        _db = db;
        _gamification = gamification;
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

        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var existing = await _db.ReadingProgresses.FindAsync(new object[] { userId, bookId }, ct);
            if (existing is null)
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
                existing.ChapterId = chapterId;
            }

            var alreadyRead = await _db.ChapterReads.FindAsync(new object[] { userId, chapterId }, ct);
            var isNewRead = alreadyRead is null;
            if (isNewRead)
            {
                _db.ChapterReads.Add(new ChapterRead { UserId = userId, ChapterId = chapterId, BookId = bookId });
            }

            await _db.SaveChangesAsync(ct);

            if (isNewRead)
            {
                await _gamification.AwardChapterReadAsync(userId, ct);
            }
        }
        else
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ReadingProgresses\" (\"UserId\", \"BookId\", \"ChapterId\", \"UpdatedAt\") VALUES ({userId}, {bookId}, {chapterId}, now()) ON CONFLICT (\"UserId\", \"BookId\") DO UPDATE SET \"ChapterId\" = EXCLUDED.\"ChapterId\", \"UpdatedAt\" = now()", ct);

            // Полный набор прочитанных глав — идемпотентная запись (допускает пропуски).
            // affected == 1 ⇒ глава прочитана впервые ⇒ начисляем XP/серию.
            var inserted = await _db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ChapterReads\" (\"UserId\", \"ChapterId\", \"BookId\", \"CreatedAt\") VALUES ({userId}, {chapterId}, {bookId}, now()) ON CONFLICT (\"UserId\", \"ChapterId\") DO NOTHING", ct);

            if (inserted == 1)
            {
                await _gamification.AwardChapterReadAsync(userId, ct);
            }
        }
    }

    public async Task<IReadOnlyList<Guid>> GetReadChapterIdsAsync(Guid userId, Guid bookId, CancellationToken ct = default)
    {
        return await _db.ChapterReads
            .AsNoTracking()
            .Where(cr => cr.UserId == userId && cr.BookId == bookId)
            .Select(cr => cr.ChapterId)
            .ToListAsync(ct);
    }

    public async Task<int> MarkReadThroughAsync(Guid userId, Guid bookId, int throughChapterNumber, CancellationToken ct = default)
    {
        var chapterIds = await _db.Chapters.AsNoTracking()
            .Where(c => c.BookId == bookId && c.IsPublished && c.ChapterNumber <= throughChapterNumber)
            .Select(c => c.Id)
            .ToListAsync(ct);
        if (chapterIds.Count == 0) return 0;

        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var existing = (await _db.ChapterReads
                .Where(cr => cr.UserId == userId && cr.BookId == bookId)
                .Select(cr => cr.ChapterId).ToListAsync(ct)).ToHashSet();
            var added = 0;
            foreach (var cid in chapterIds)
            {
                if (existing.Contains(cid)) continue;
                _db.ChapterReads.Add(new ChapterRead { UserId = userId, ChapterId = cid, BookId = bookId });
                added++;
            }
            await _db.SaveChangesAsync(ct);
            return added;
        }

        // Идемпотентная пачка: считаем суммарно вставленные строки.
        var inserted = 0;
        foreach (var cid in chapterIds)
        {
            inserted += await _db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO \"ChapterReads\" (\"UserId\", \"ChapterId\", \"BookId\", \"CreatedAt\") VALUES ({userId}, {cid}, {bookId}, now()) ON CONFLICT (\"UserId\", \"ChapterId\") DO NOTHING", ct);
        }
        return inserted;
    }

    public async Task<IReadOnlyList<ContinueReadingDto>> GetContinueReadingAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 12 : Math.Min(limit, 24);
        return await _db.ReadingProgresses
            .AsNoTracking()
            .Where(rp => rp.UserId == userId)
            .OrderByDescending(rp => rp.UpdatedAt)
            .Take(take)
            .Select(rp => new ContinueReadingDto(
                rp.BookId,
                rp.Book.Title,
                rp.Book.CoverUrl,
                rp.ChapterId,
                rp.Chapter.ChapterNumber,
                rp.UpdatedAt))
            .ToListAsync(ct);
    }
}
