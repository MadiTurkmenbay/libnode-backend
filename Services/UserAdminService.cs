using LibNode.Api.Data;
using LibNode.Api.Exceptions;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class UserAdminService : IUserAdminService
{
    private const int MaxLimit = 50;
    private readonly AppDbContext _db;

    public UserAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CursorPagedResult<AdminUserDto, Guid>> ListAsync(string? search, Guid? cursor, int limit, CancellationToken ct = default)
    {
        var take = limit <= 0 ? 25 : Math.Min(limit, MaxLimit);

        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Username, term) || EF.Functions.ILike(u.Email, term));
        }

        if (cursor.HasValue)
        {
            query = query.Where(u => u.Id.CompareTo(cursor.Value) < 0);
        }

        var rows = await query
            .OrderByDescending(u => u.Id)
            .Take(take + 1)
            .Select(u => new AdminUserDto(u.Id, u.Username, u.Email, u.Role, u.CreatedAt, u.IsBanned, u.IsMuted))
            .ToListAsync(ct);

        var hasMore = rows.Count > take;
        if (hasMore) rows.RemoveAt(rows.Count - 1);
        Guid? nextCursor = hasMore && rows.Count > 0 ? rows[^1].Id : null;
        return new CursorPagedResult<AdminUserDto, Guid>(rows, nextCursor, hasMore);
    }

    public async Task<AdminUserDto?> UpdateRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return null;

        if (user.Role == role)
            return new AdminUserDto(user.Id, user.Username, user.Email, user.Role, user.CreatedAt, user.IsBanned, user.IsMuted);

        // Нельзя разжаловать последнего администратора (иначе lockout системы).
        if (user.Role == "Admin" && role != "Admin")
        {
            var adminCount = await _db.Users.CountAsync(u => u.Role == "Admin", ct);
            if (adminCount <= 1)
                throw new ConflictException("Нельзя разжаловать последнего администратора.");
        }

        user.Role = role;
        // Смена роли инвалидирует ранее выданные токены (разжалованный admin теряет доступ).
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);
        return new AdminUserDto(user.Id, user.Username, user.Email, user.Role, user.CreatedAt, user.IsBanned, user.IsMuted);
    }

    public async Task<AdminUserDto?> ToggleBanAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return null;
        user.IsBanned = !user.IsBanned;
        if (user.IsBanned)
            // Бан инвалидирует ранее выданные токены пользователя.
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);
        return new AdminUserDto(user.Id, user.Username, user.Email, user.Role, user.CreatedAt, user.IsBanned, user.IsMuted);
    }

    public async Task<AdminUserDto?> ToggleMuteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return null;
        user.IsMuted = !user.IsMuted;
        await _db.SaveChangesAsync(ct);
        return new AdminUserDto(user.Id, user.Username, user.Email, user.Role, user.CreatedAt, user.IsBanned, user.IsMuted);
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
