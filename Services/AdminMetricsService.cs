using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class AdminMetricsService : IAdminMetricsService
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public AdminMetricsService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<AdminMetricsDto> GetMetricsAsync(CancellationToken ct = default)
    {
        var userCount = await _db.Users.CountAsync(ct);
        var bookCount = await _db.Books.CountAsync(ct);
        var chapterCount = await _db.Chapters.CountAsync(ct);
        var commentCount = await _db.Comments.CountAsync(ct);
        var reportCount = await _db.CommentReports.CountAsync(r => !r.IsResolved, ct);
        var teamCount = await _db.Teams.CountAsync(ct);
        var pendingRequests = await _db.TeamTitleRequests.CountAsync(r => r.Status == RequestStatus.Pending, ct);
        var pendingInvites = await _db.TeamInvites.CountAsync(i => i.Status == RequestStatus.Pending, ct);

        var recentSignupsRaw = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new { u.Id, u.Username, u.AvatarUrl, u.CreatedAt })
            .ToListAsync(ct);

        var recentSignups = recentSignupsRaw
            .Select(u => new AdminRecentSignupDto(
                u.Id,
                u.Username,
                u.AvatarUrl == null ? null : _storage.ResolveUrl(u.AvatarUrl),
                u.CreatedAt))
            .ToList();

        return new AdminMetricsDto(
            userCount,
            bookCount,
            chapterCount,
            commentCount,
            reportCount,
            teamCount,
            pendingRequests,
            pendingInvites,
            recentSignups);
    }
}
