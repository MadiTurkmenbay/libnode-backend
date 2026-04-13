using LibNode.Api.Data;
using LibNode.Api.Exceptions;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibNode.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;

    public CategoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var category = new Category
        {
            Name = dto.Name,
            Slug = dto.Slug
        };

        _db.Categories.Add(category);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException($"Категория со slug '{dto.Slug}' уже существует.");
        }

        return new CategoryDto(category.Id, category.Name, category.Slug);
    }

    public async Task<CategoryDto?> UpdateAsync(Guid id, CreateCategoryDto dto, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct);

        if (category is null)
        {
            return null;
        }

        category.Name = dto.Name;
        category.Slug = dto.Slug;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException($"Категория со slug '{dto.Slug}' уже существует.");
        }

        return new CategoryDto(category.Id, category.Name, category.Slug);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FindAsync([id], ct);

        if (category is null)
        {
            return false;
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
