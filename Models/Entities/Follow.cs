using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Подписка пользователя на сущность (другого пользователя или команду переводчиков).
/// Составной ключ (FollowerUserId, TargetType, TargetId) исключает дубли.
/// </summary>
public class Follow
{
    public Guid FollowerUserId { get; set; }
    public User Follower { get; set; } = null!;

    public FollowTargetType TargetType { get; set; }

    /// <summary>Id цели подписки (UserId или TeamId в зависимости от TargetType).</summary>
    public Guid TargetId { get; set; }

    public DateTime CreatedAt { get; set; }
}
