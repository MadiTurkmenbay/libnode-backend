using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface IUserAdminService
{
    Task<CursorPagedResult<AdminUserDto, Guid>> ListAsync(string? search, Guid? cursor, int limit, CancellationToken ct = default);
    Task<AdminUserDto?> UpdateRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<AdminUserDto?> ToggleBanAsync(Guid userId, CancellationToken ct = default);
    Task<AdminUserDto?> ToggleMuteAsync(Guid userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default);
}
