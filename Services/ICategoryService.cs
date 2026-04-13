using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
    Task<CategoryDto?> UpdateAsync(Guid id, CreateCategoryDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
