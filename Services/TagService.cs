using LibNode.Api.Data;
using LibNode.Api.Exceptions;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibNode.Api.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _db;

    public TagService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TagDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Tags
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Slug))
            .ToListAsync(ct);
    }

    public async Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct = default)
    {
        var tag = new Tag
        {
            Name = dto.Name,
            Slug = dto.Slug
        };

        _db.Tags.Add(tag);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException($"Тег со slug '{dto.Slug}' уже существует.");
        }

        return new TagDto(tag.Id, tag.Name, tag.Slug);
    }

    public async Task<TagDto?> UpdateAsync(Guid id, CreateTagDto dto, CancellationToken ct = default)
    {
        var tag = await _db.Tags.FindAsync([id], ct);

        if (tag is null)
        {
            return null;
        }

        tag.Name = dto.Name;
        tag.Slug = dto.Slug;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException($"Тег со slug '{dto.Slug}' уже существует.");
        }

        return new TagDto(tag.Id, tag.Name, tag.Slug);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tag = await _db.Tags.FindAsync([id], ct);

        if (tag is null)
        {
            return false;
        }

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
