using LibNode.Api.Data;
using LibNode.Api.Services;
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
    private readonly IStorageService _storage;

    public AdminMetricsController(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
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

        var recentSignupsRaw = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.CreatedAt })
            .ToListAsync(ct);

        // Ключ аватара → абсолютный URL (в памяти).
        var recentSignups = recentSignupsRaw
            .Select(u => new
            {
                u.Id,
                u.Username,
                AvatarUrl = u.AvatarUrl == null ? null : _storage.ResolveUrl(u.AvatarUrl),
                u.CreatedAt,
            })
            .ToList();

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
