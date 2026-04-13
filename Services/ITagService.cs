using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface ITagService
{
    Task<List<TagDto>> GetAllAsync(CancellationToken ct = default);
    Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct = default);
    Task<TagDto?> UpdateAsync(Guid id, CreateTagDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
