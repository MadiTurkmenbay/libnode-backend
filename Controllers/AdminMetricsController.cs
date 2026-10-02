using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/admin/metrics")]
[Authorize(Roles = "Admin")]
public class AdminMetricsController : ControllerBase
{
    private readonly IAdminMetricsService _metrics;

    public AdminMetricsController(IAdminMetricsService metrics)
    {
        _metrics = metrics;
    }

    [HttpGet]
    public async Task<ActionResult<AdminMetricsDto>> GetMetrics(CancellationToken ct)
        => Ok(await _metrics.GetMetricsAsync(ct));
}
