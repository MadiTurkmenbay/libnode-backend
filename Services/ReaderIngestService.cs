using System.Text;
using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibNode.Api.Services;

public class ReaderIngestService : IReaderIngestService
{
    private readonly AppDbContext _db;

    public ReaderIngestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(ReaderResourceDto Resource, bool Created)> CreateOrGetTitleAsync(
        ReaderTitlePayloadDto dto,
        CancellationToken ct = default)
    {
        var normalizedSlug = NormalizeSlug(dto.Slug);
        var mappedType = MapBookType(dto.Language);

        var existing = await _db.Books
            .FirstOrDefaultAsync(b => b.Slug == normalizedSlug, ct);

        if (existing is not null)
        {
            var updated = false;

            if (!string.Equals(existing.Title, dto.Title, StringComparison.Ordinal))
            {
                existing.Title = dto.Title;
                updated = true;
            }

            if (!string.Equals(existing.Description, dto.Description, StringComparison.Ordinal))
            {
                existing.Description = dto.Description;
                updated = true;
            }

            if (existing.Type != mappedType)
            {
                existing.Type = mappedType;
                updated = true;
            }

            if (existing.TranslationStatus == TranslationStatus.None)
            {
                existing.TranslationStatus = TranslationStatus.Ongoing;
                updated = true;
            }

            if (updated)
            {
                await _db.SaveChangesAsync(ct);
            }

            return (new ReaderResourceDto(existing.Id), false);
        }

        var book = new Book
        {
            Id = Guid.CreateVersion7(),
            Title = dto.Title,
            Slug = normalizedSlug,
            Description = dto.Description,
            Type = mappedType,
            OriginalStatus = OriginalStatus.None,
            TranslationStatus = TranslationStatus.Ongoing
        };

        try
        {
            _db.Books.Add(book);
            await _db.SaveChangesAsync(ct);
            return (new ReaderResourceDto(book.Id), true);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            var conflictBook = await _db.Books
                .AsNoTracking()
                .FirstAsync(b => b.Slug == normalizedSlug, ct);

            return (new ReaderResourceDto(conflictBook.Id), false);
        }
    }

    public async Task<(ReaderResourceDto Resource, bool Created)> CreateOrUpdateChapterAsync(
        ReaderChapterPayloadDto dto,
        CancellationToken ct = default)
    {
        var bookExists = await _db.Books
            .AsNoTracking()
            .AnyAsync(b => b.Id == dto.ReaderTitleId, ct);

        if (!bookExists)
        {
            throw new KeyNotFoundException($"Book with id {dto.ReaderTitleId} was not found.");
        }

        var existing = await _db.Chapters
            .FirstOrDefaultAsync(
                c => c.BookId == dto.ReaderTitleId && c.ChapterNumber == dto.ChapterNumber,
                ct);

        if (existing is not null)
        {
            existing.Title = dto.Title;
            existing.Content = dto.Body;
            await _db.SaveChangesAsync(ct);
            return (new ReaderResourceDto(existing.Id), false);
        }

        var chapter = new Chapter
        {
            Id = Guid.CreateVersion7(),
            BookId = dto.ReaderTitleId,
            ChapterNumber = dto.ChapterNumber,
            Title = dto.Title,
            Content = dto.Body
        };

        try
        {
            _db.Chapters.Add(chapter);
            await _db.SaveChangesAsync(ct);
            return (new ReaderResourceDto(chapter.Id), true);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            var conflictChapter = await _db.Chapters
                .AsNoTracking()
                .FirstAsync(
                    c => c.BookId == dto.ReaderTitleId && c.ChapterNumber == dto.ChapterNumber,
                    ct);

            return (new ReaderResourceDto(conflictChapter.Id), false);
        }
    }

    private static BookType MapBookType(string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "ko" or "ko-kr" => BookType.Korea,
            "zh" or "zh-cn" or "zh-tw" => BookType.China,
            "en" or "en-us" or "en-gb" => BookType.English,
            _ => BookType.Original
        };
    }

    private static string NormalizeSlug(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasDash = false;

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousWasDash = false;
                continue;
            }

            if (previousWasDash)
            {
                continue;
            }

            builder.Append('-');
            previousWasDash = true;
        }

        var normalized = builder.ToString().Trim('-');
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Slug must contain at least one letter or digit.");
        }

        return normalized.Length <= 300 ? normalized : normalized[..300];
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
