using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface IAdminMetricsService
{
    Task<AdminMetricsDto> GetMetricsAsync(CancellationToken ct = default);
}
