using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Services;

public class TeamService : ITeamService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IStorageService _storage;

    public TeamService(AppDbContext db, INotificationService notifications, IStorageService storage)
    {
        _db = db;
        _notifications = notifications;
        _storage = storage;
    }

    // ── Admin ────────────────────────────────────────────

    public async Task<IReadOnlyList<TeamDto>> ListTeamsAsync(CancellationToken ct = default)
    {
        return await _db.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto(t.Id, t.Name, t.Slug, t.Description, t.Members.Count, t.Books.Count, t.CreatedAt, t.IsVerified))
            .ToListAsync(ct);
    }

    public async Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default)
    {
        var team = new Team
        {
            Name = dto.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(dto.Slug) ? null : dto.Slug.Trim().ToLowerInvariant(),
            Description = dto.Description,
        };
        _db.Teams.Add(team);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Команда с таким slug уже существует.");
        }
        return new TeamDto(team.Id, team.Name, team.Slug, team.Description, 0, 0, team.CreatedAt, team.IsVerified);
    }

    public async Task<TeamDto?> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto, CancellationToken ct = default)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return null;

        team.Name = dto.Name.Trim();
        team.Description = dto.Description;
        await _db.SaveChangesAsync(ct);

        var counts = await _db.Teams.Where(t => t.Id == teamId)
            .Select(t => new { Members = t.Members.Count, Books = t.Books.Count })
            .FirstAsync(ct);
        return new TeamDto(team.Id, team.Name, team.Slug, team.Description, counts.Members, counts.Books, team.CreatedAt, team.IsVerified);
    }

    public async Task<bool> DeleteTeamAsync(Guid teamId, CancellationToken ct = default)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return false;
        _db.Teams.Remove(team);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TeamDto?> ToggleVerifyAsync(Guid teamId, CancellationToken ct = default)
    {
        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == teamId, ct);
        if (team == null) return null;
        team.IsVerified = !team.IsVerified;
        await _db.SaveChangesAsync(ct);
        return new TeamDto(team.Id, team.Name, team.Slug, team.Description, 0, 0, team.CreatedAt, team.IsVerified);
    }

    public async Task<IReadOnlyList<TeamTitleRequestDto>> ListAllRequestsAsync(RequestStatus? status, CancellationToken ct = default)
    {
        var query = _db.TeamTitleRequests.AsNoTracking();
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new TeamTitleRequestDto(
                r.Id, r.TeamId, r.Team.Name, r.BookId, r.Book.Title, r.Status, r.Message, r.CreatedAt, r.DecidedAt))
            .ToListAsync(ct);
    }

    public async Task<TeamTitleRequestDto?> DecideRequestAsync(Guid requestId, bool approve, CancellationToken ct = default)
    {
        var request = await _db.TeamTitleRequests
            .Include(r => r.Team)
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);
        if (request == null) return null;

        request.Status = approve ? RequestStatus.Approved : RequestStatus.Rejected;
        request.DecidedAt = DateTime.UtcNow;

        if (approve)
        {
            // Закрепляем тайтл за командой и отклоняем остальные ожидающие заявки на эту книгу.
            await _db.Books.Where(b => b.Id == request.BookId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.TeamId, request.TeamId), ct);

            await _db.TeamTitleRequests
                .Where(r => r.BookId == request.BookId && r.Id != requestId && r.Status == RequestStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, RequestStatus.Rejected)
                    .SetProperty(r => r.DecidedAt, DateTime.UtcNow), ct);
        }

        await _db.SaveChangesAsync(ct);

        // Уведомляем глав команды о решении.
        var headIds = await _db.TeamMembers
            .Where(m => m.TeamId == request.TeamId && m.Role == TeamRole.Head)
            .Select(m => m.UserId)
            .ToListAsync(ct);
        await _notifications.CreateManyAsync(
            headIds,
            approve ? NotificationType.RequestApproved : NotificationType.RequestRejected,
            approve ? $"Заявка на «{request.Book.Title}» одобрена" : $"Заявка на «{request.Book.Title}» отклонена",
            null,
            $"/admin/teams/{request.TeamId}",
            ct);

        return new TeamTitleRequestDto(
            request.Id, request.TeamId, request.Team.Name, request.BookId, request.Book.Title,
            request.Status, request.Message, request.CreatedAt, request.DecidedAt);
    }

    // ── Team / members ───────────────────────────────────

    public async Task<TeamDetailDto?> GetTeamAsync(Guid teamId, Guid? currentUserId, CancellationToken ct = default)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new
            {
                t.Id, t.Name, t.Slug, t.Description, t.CreatedAt, t.IsVerified,
                Members = t.Members.OrderBy(m => m.Role).Select(m => new TeamMemberDto(m.UserId, m.User.Username, m.Role, m.CreatedAt)).ToList(),
                Books = t.Books.Select(b => new TeamBookDto(b.Id, b.Title, b.CoverUrl, b.Chapters.Count)).ToList(),
            })
            .FirstOrDefaultAsync(ct);
        if (team == null) return null;

        TeamRole? myRole = currentUserId.HasValue
            ? team.Members.FirstOrDefault(m => m.UserId == currentUserId.Value)?.Role
            : null;

        // Ключи обложек → абсолютные URL (в памяти).
        var books = team.Books
            .Select(b => b with { CoverUrl = b.CoverUrl == null ? null : _storage.ResolveUrl(b.CoverUrl) })
            .ToList();

        return new TeamDetailDto(team.Id, team.Name, team.Slug, team.Description, team.CreatedAt, team.Members, books, myRole, team.IsVerified);
    }

    public async Task<IReadOnlyList<TeamDto>> GetMyTeamsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.TeamMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new TeamDto(m.Team.Id, m.Team.Name, m.Team.Slug, m.Team.Description, m.Team.Members.Count, m.Team.Books.Count, m.Team.CreatedAt, m.Team.IsVerified))
            .ToListAsync(ct);
    }

    private async Task EnsureCanManageTeamAsync(Guid teamId, Guid actingUserId, bool isAdmin, CancellationToken ct)
    {
        if (isAdmin) return;
        var isHead = await _db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == actingUserId && m.Role == TeamRole.Head, ct);
        if (!isHead)
            throw new UnauthorizedAccessException("Управлять командой может только глава или администратор.");
    }

    public async Task CreateInviteAsync(Guid teamId, Guid actingUserId, bool isAdmin, string username, TeamRole role, CancellationToken ct = default)
    {
        await EnsureCanManageTeamAsync(teamId, actingUserId, isAdmin, ct);

        var team = await _db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, ct)
            ?? throw new KeyNotFoundException("Команда не найдена.");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username, ct)
            ?? throw new KeyNotFoundException($"Пользователь '{username}' не найден.");

        var already = await _db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == user.Id, ct);
        if (already) throw new InvalidOperationException("Пользователь уже в команде.");

        var pending = await _db.TeamInvites.AnyAsync(i => i.TeamId == teamId && i.UserId == user.Id && i.Status == RequestStatus.Pending, ct);
        if (pending) throw new InvalidOperationException("Приглашение уже отправлено.");

        _db.TeamInvites.Add(new TeamInvite { TeamId = teamId, UserId = user.Id, Role = role, Status = RequestStatus.Pending });
        await _db.SaveChangesAsync(ct);

        await _notifications.CreateAsync(
            user.Id,
            NotificationType.TeamInvite,
            $"Приглашение в команду «{team.Name}»",
            "Откройте профиль, чтобы принять или отклонить.",
            "/profile/invites",
            ct);
    }

    public async Task<IReadOnlyList<TeamInviteDto>> GetMyInvitesAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.TeamInvites
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Status == RequestStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new TeamInviteDto(i.Id, i.TeamId, i.Team.Name, i.Role, i.Status, i.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task AcceptInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default)
    {
        var invite = await _db.TeamInvites.FirstOrDefaultAsync(i => i.Id == inviteId && i.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Приглашение не найдено.");
        if (invite.Status != RequestStatus.Pending)
            throw new InvalidOperationException("Приглашение уже обработано.");

        var already = await _db.TeamMembers.AnyAsync(m => m.TeamId == invite.TeamId && m.UserId == userId, ct);
        if (!already)
        {
            _db.TeamMembers.Add(new TeamMember { TeamId = invite.TeamId, UserId = userId, Role = invite.Role });
        }

        invite.Status = RequestStatus.Approved;
        invite.DecidedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeclineInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default)
    {
        var invite = await _db.TeamInvites.FirstOrDefaultAsync(i => i.Id == inviteId && i.UserId == userId, ct);
        if (invite == null || invite.Status != RequestStatus.Pending) return;
        invite.Status = RequestStatus.Rejected;
        invite.DecidedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateMemberRoleAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid targetUserId, TeamRole role, CancellationToken ct = default)
    {
        await EnsureCanManageTeamAsync(teamId, actingUserId, isAdmin, ct);

        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == targetUserId, ct)
            ?? throw new KeyNotFoundException("Участник не найден.");

        // Нельзя снять последнего главу.
        if (member.Role == TeamRole.Head && role != TeamRole.Head)
        {
            var headCount = await _db.TeamMembers.CountAsync(m => m.TeamId == teamId && m.Role == TeamRole.Head, ct);
            if (headCount <= 1) throw new InvalidOperationException("Нельзя снять единственного главу команды.");
        }

        member.Role = role;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid targetUserId, CancellationToken ct = default)
    {
        await EnsureCanManageTeamAsync(teamId, actingUserId, isAdmin, ct);

        var member = await _db.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == targetUserId, ct);
        if (member == null) return;

        if (member.Role == TeamRole.Head)
        {
            var headCount = await _db.TeamMembers.CountAsync(m => m.TeamId == teamId && m.Role == TeamRole.Head, ct);
            if (headCount <= 1) throw new InvalidOperationException("Нельзя удалить единственного главу команды.");
        }

        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync(ct);
    }

    // ── Title requests ───────────────────────────────────

    public async Task<TeamTitleRequestDto> RequestTitleAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid bookId, string? message, CancellationToken ct = default)
    {
        await EnsureCanManageTeamAsync(teamId, actingUserId, isAdmin, ct);

        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId, ct)
            ?? throw new KeyNotFoundException("Тайтл не найден.");

        if (book.TeamId == teamId)
            throw new InvalidOperationException("Тайтл уже закреплён за вашей командой.");

        var dup = await _db.TeamTitleRequests.AnyAsync(
            r => r.TeamId == teamId && r.BookId == bookId && r.Status == RequestStatus.Pending, ct);
        if (dup) throw new InvalidOperationException("Заявка на этот тайтл уже отправлена.");

        var request = new TeamTitleRequest
        {
            TeamId = teamId,
            BookId = bookId,
            Status = RequestStatus.Pending,
            Message = message,
        };
        _db.TeamTitleRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        var teamName = await _db.Teams.Where(t => t.Id == teamId).Select(t => t.Name).FirstAsync(ct);
        return new TeamTitleRequestDto(request.Id, teamId, teamName, bookId, book.Title, request.Status, request.Message, request.CreatedAt, null);
    }

    // ── Authorization helpers ────────────────────────────

    public async Task<TeamRole?> GetUserRoleInBookTeamAsync(Guid userId, Guid bookId, CancellationToken ct = default)
    {
        var teamId = await _db.Books.Where(b => b.Id == bookId).Select(b => b.TeamId).FirstOrDefaultAsync(ct);
        if (teamId == null) return null;

        var member = await _db.TeamMembers.AsNoTracking()
            .Where(m => m.TeamId == teamId.Value && m.UserId == userId)
            .Select(m => (TeamRole?)m.Role)
            .FirstOrDefaultAsync(ct);
        return member;
    }

    public async Task<bool> CanEditBookChaptersAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default)
    {
        if (isAdmin) return true;
        var role = await GetUserRoleInBookTeamAsync(userId, bookId, ct);
        return role.HasValue;
    }

    public async Task<bool> CanManageBookTitleAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default)
    {
        if (isAdmin) return true;
        var role = await GetUserRoleInBookTeamAsync(userId, bookId, ct);
        return role == TeamRole.Head;
    }

    public async Task<BookTeamDto?> GetBookTeamAsync(Guid bookId, CancellationToken ct = default)
    {
        return await _db.Books
            .AsNoTracking()
            .Where(b => b.Id == bookId && b.TeamId != null)
            .Select(b => new BookTeamDto(b.Team!.Id, b.Team.Name, b.Team.Slug))
            .FirstOrDefaultAsync(ct);
    }
}
