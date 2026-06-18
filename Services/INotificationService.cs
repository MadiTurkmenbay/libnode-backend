using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Services;

public interface INotificationService
{
    Task CreateAsync(Guid userId, NotificationType type, string title, string? message, string? linkUrl, CancellationToken ct = default);
    Task CreateManyAsync(IEnumerable<Guid> userIds, NotificationType type, string title, string? message, string? linkUrl, CancellationToken ct = default);

    Task<CursorPagedResult<NotificationDto, Guid>> ListAsync(Guid userId, Guid? cursor, int limit, bool? isRead = null, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
