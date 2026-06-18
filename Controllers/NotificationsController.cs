using System.Security.Claims;
using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly AppDbContext _db;

    public NotificationsController(INotificationService notifications, AppDbContext db)
    {
        _notifications = notifications;
        _db = db;
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    [HttpGet]
    public async Task<ActionResult<CursorPagedResult<NotificationDto, Guid>>> List(
        [FromQuery] Guid? cursor, [FromQuery] int limit, [FromQuery] bool? isRead, CancellationToken ct)
        => Ok(await _notifications.ListAsync(GetUserId(), cursor, limit, isRead, ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> UnreadCount(CancellationToken ct)
        => Ok(new { count = await _notifications.UnreadCountAsync(GetUserId(), ct) });

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        var userId = GetUserId();

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        var lastCount = await _notifications.UnreadCountAsync(userId, ct);
        await WriteEvent("count", $"{{\"count\":{lastCount}}}", ct);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        var lastKeepalive = DateTime.UtcNow;

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                var now = DateTime.UtcNow;
                if ((now - lastKeepalive).TotalSeconds >= 30)
                {
                    await Response.WriteAsync(": keepalive\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                    lastKeepalive = now;
                }

                var currentCount = await _notifications.UnreadCountAsync(userId, ct);
                if (currentCount != lastCount)
                {
                    lastCount = currentCount;
                    await WriteEvent("count", $"{{\"count\":{currentCount}}}", ct);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { }
    }

    private async Task WriteEvent(string eventType, string data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {eventType}\n", ct);
        await Response.WriteAsync($"data: {data}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllReadAsync(GetUserId(), ct);
        return NoContent();
    }

    [HttpGet("prefs")]
    public async Task<ActionResult<NotificationPrefsDto>> GetPrefs(CancellationToken ct)
    {
        var userId = GetUserId();
        var prefs = await _db.UserNotificationPrefs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (prefs is null)
            return Ok(new NotificationPrefsDto(true, true, true, true, true, true, true, true));

        return Ok(new NotificationPrefsDto(
            prefs.EnableCommentReply, prefs.EnableTeamInvite,
            prefs.EnableRequestApproved, prefs.EnableRequestRejected,
            prefs.EnableNewChapter, prefs.EnableMention,
            prefs.EnableLevelUp, prefs.EnableAchievement));
    }

    [HttpPut("prefs")]
    public async Task<ActionResult<NotificationPrefsDto>> UpdatePrefs(
        [FromBody] UpdateNotificationPrefsDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var prefs = await _db.UserNotificationPrefs
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (prefs is null)
        {
            prefs = new UserNotificationPrefs { UserId = userId };
            _db.UserNotificationPrefs.Add(prefs);
        }

        if (dto.EnableCommentReply.HasValue) prefs.EnableCommentReply = dto.EnableCommentReply.Value;
        if (dto.EnableTeamInvite.HasValue) prefs.EnableTeamInvite = dto.EnableTeamInvite.Value;
        if (dto.EnableRequestApproved.HasValue) prefs.EnableRequestApproved = dto.EnableRequestApproved.Value;
        if (dto.EnableRequestRejected.HasValue) prefs.EnableRequestRejected = dto.EnableRequestRejected.Value;
        if (dto.EnableNewChapter.HasValue) prefs.EnableNewChapter = dto.EnableNewChapter.Value;
        if (dto.EnableMention.HasValue) prefs.EnableMention = dto.EnableMention.Value;
        if (dto.EnableLevelUp.HasValue) prefs.EnableLevelUp = dto.EnableLevelUp.Value;
        if (dto.EnableAchievement.HasValue) prefs.EnableAchievement = dto.EnableAchievement.Value;
        prefs.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new NotificationPrefsDto(
            prefs.EnableCommentReply, prefs.EnableTeamInvite,
            prefs.EnableRequestApproved, prefs.EnableRequestRejected,
            prefs.EnableNewChapter, prefs.EnableMention,
            prefs.EnableLevelUp, prefs.EnableAchievement));
    }
}
