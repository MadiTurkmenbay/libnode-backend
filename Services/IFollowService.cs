using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Services;

/// <summary>Подписки на пользователей/команды + лента активности.</summary>
public interface IFollowService
{
    Task<FollowStatusDto> FollowAsync(Guid userId, FollowTargetType type, Guid targetId, CancellationToken ct = default);
    Task<FollowStatusDto> UnfollowAsync(Guid userId, FollowTargetType type, Guid targetId, CancellationToken ct = default);
    Task<FollowStatusDto> GetStatusAsync(Guid? userId, FollowTargetType type, Guid targetId, CancellationToken ct = default);

    /// <summary>Лента: новые опубликованные главы книг отслеживаемых команд (cursor по Chapter.Id desc).</summary>
    Task<CursorPagedResult<FeedItemDto, Guid>> GetFeedAsync(Guid userId, Guid? cursor, int limit, CancellationToken ct = default);
}
