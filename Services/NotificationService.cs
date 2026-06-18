using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class NotificationService : INotificationService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateAsync(Guid userId, NotificationType type, string title, string? message, string? linkUrl, CancellationToken ct = default)
    {
        if (!await IsEnabledForUserAsync(userId, type, ct)) return;

        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateManyAsync(IEnumerable<Guid> userIds, NotificationType type, string title, string? message, string? linkUrl, CancellationToken ct = default)
    {
        var distinct = userIds.Distinct().ToList();
        if (distinct.Count == 0) return;

        var prefs = await _db.UserNotificationPrefs
            .AsNoTracking()
            .Where(p => distinct.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, ct);

        var filtered = distinct.Where(uid => IsEnabled(type, prefs.GetValueOrDefault(uid))).ToList();
        if (filtered.Count == 0) return;

        foreach (var uid in filtered)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = uid,
                Type = type,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool> IsEnabledForUserAsync(Guid userId, NotificationType type, CancellationToken ct)
    {
        var prefs = await _db.UserNotificationPrefs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
        return IsEnabled(type, prefs);
    }

    private static bool IsEnabled(NotificationType type, UserNotificationPrefs? prefs)
    {
        if (prefs is null) return true;
        return type switch
        {
            NotificationType.CommentReply => prefs.EnableCommentReply,
            NotificationType.TeamInvite => prefs.EnableTeamInvite,
            NotificationType.RequestApproved => prefs.EnableRequestApproved,
            NotificationType.RequestRejected => prefs.EnableRequestRejected,
            NotificationType.NewChapter => prefs.EnableNewChapter,
            NotificationType.Mention => prefs.EnableMention,
            NotificationType.LevelUp => prefs.EnableLevelUp,
            NotificationType.Achievement => prefs.EnableAchievement,
            _ => true,
        };
    }

    public async Task<CursorPagedResult<NotificationDto, Guid>> ListAsync(Guid userId, Guid? cursor, int limit, bool? isRead = null, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, MaxLimit);

        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (isRead.HasValue) query = query.Where(n => n.IsRead == isRead.Value);
        if (cursor.HasValue) query = query.Where(n => n.Id.CompareTo(cursor.Value) < 0);

        var rows = await query
            .OrderByDescending(n => n.Id)
            .Take(take + 1)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Message, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        Guid? next = hasMore && rows.Count > 0 ? rows[^1].Id : null;
        return new CursorPagedResult<NotificationDto, Guid>(rows, next, hasMore);
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
