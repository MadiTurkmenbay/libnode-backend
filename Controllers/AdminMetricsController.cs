using LibNode.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/admin/metrics")]
[Authorize(Roles = "Admin")]
public class AdminMetricsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminMetricsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetMetrics(CancellationToken ct)
    {
        var userCount = await _db.Users.CountAsync(ct);
        var bookCount = await _db.Books.CountAsync(ct);
        var chapterCount = await _db.Chapters.CountAsync(ct);
        var commentCount = await _db.Comments.CountAsync(ct);
        var reportCount = await _db.CommentReports.CountAsync(r => !r.IsResolved, ct);
        var teamCount = await _db.Teams.CountAsync(ct);
        var pendingRequests = await _db.TeamTitleRequests.CountAsync(r => r.Status == 0, ct);
        var pendingInvites = await _db.TeamInvites.CountAsync(i => i.Status == 0, ct);

        var recentSignups = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.CreatedAt })
            .ToListAsync(ct);

        return Ok(new
        {
            userCount,
            bookCount,
            chapterCount,
            commentCount,
            reportCount,
            teamCount,
            pendingRequests,
            pendingInvites,
            recentSignups,
        });
    }
}
