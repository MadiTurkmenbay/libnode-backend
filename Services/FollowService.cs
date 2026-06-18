using LibNode.Api.Data;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class FollowService : IFollowService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;

    public FollowService(AppDbContext db)
    {
        _db = db;
    }

    private Task<int> FollowerCountAsync(FollowTargetType type, Guid targetId, CancellationToken ct) =>
        _db.Follows.CountAsync(f => f.TargetType == type && f.TargetId == targetId, ct);

    private async Task<bool> TargetExistsAsync(FollowTargetType type, Guid targetId, CancellationToken ct) =>
        type switch
        {
            FollowTargetType.User => await _db.Users.AnyAsync(u => u.Id == targetId, ct),
            FollowTargetType.Team => await _db.Teams.AnyAsync(t => t.Id == targetId, ct),
            _ => false,
        };

    public async Task<FollowStatusDto> FollowAsync(Guid userId, FollowTargetType type, Guid targetId, CancellationToken ct = default)
    {
        if (type == FollowTargetType.User && targetId == userId)
            throw new InvalidOperationException("Нельзя подписаться на самого себя.");
        if (!await TargetExistsAsync(type, targetId, ct))
            throw new KeyNotFoundException("Цель подписки не найдена.");

        var exists = await _db.Follows.AnyAsync(
            f => f.FollowerUserId == userId && f.TargetType == type && f.TargetId == targetId, ct);
        if (!exists)
        {
            _db.Follows.Add(new Follow { FollowerUserId = userId, TargetType = type, TargetId = targetId });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { /* гонка: уже подписан */ }
        }

        return new FollowStatusDto(true, await FollowerCountAsync(type, targetId, ct));
    }

    public async Task<FollowStatusDto> UnfollowAsync(Guid userId, FollowTargetType type, Guid targetId, CancellationToken ct = default)
    {
        var row = await _db.Follows.FirstOrDefaultAsync(
            f => f.FollowerUserId == userId && f.TargetType == type && f.TargetId == targetId, ct);
        if (row != null)
        {
            _db.Follows.Remove(row);
            await _db.SaveChangesAsync(ct);
        }

        return new FollowStatusDto(false, await FollowerCountAsync(type, targetId, ct));
    }

    public async Task<FollowStatusDto> GetStatusAsync(Guid? userId, FollowTargetType type, Guid targetId, CancellationToken ct = default)
    {
        var isFollowing = userId.HasValue && await _db.Follows.AnyAsync(
            f => f.FollowerUserId == userId.Value && f.TargetType == type && f.TargetId == targetId, ct);
        return new FollowStatusDto(isFollowing, await FollowerCountAsync(type, targetId, ct));
    }

    public async Task<CursorPagedResult<FeedItemDto, Guid>> GetFeedAsync(Guid userId, Guid? cursor, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 20 : Math.Min(limit, MaxLimit);

        var followedTeamIds = _db.Follows
            .Where(f => f.FollowerUserId == userId && f.TargetType == FollowTargetType.Team)
            .Select(f => f.TargetId);

        var query = _db.Chapters.AsNoTracking()
            .Where(c => c.IsPublished
                && c.Book.TeamId != null
                && followedTeamIds.Contains(c.Book.TeamId.Value));

        if (cursor.HasValue)
            query = query.Where(c => c.Id.CompareTo(cursor.Value) < 0);

        var rows = await query
            .OrderByDescending(c => c.Id)
            .Take(take + 1)
            .Select(c => new FeedItemDto(
                c.Id,
                c.BookId,
                c.Book.Title,
                c.Book.CoverThumbUrl,
                c.ChapterNumber,
                c.Title,
                c.Book.TeamId,
                c.Book.Team != null ? c.Book.Team.Name : null,
                c.CreatedAt))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        Guid? next = hasMore && rows.Count > 0 ? rows[^1].ChapterId : null;
        return new CursorPagedResult<FeedItemDto, Guid>(rows, next, hasMore);
    }
}
