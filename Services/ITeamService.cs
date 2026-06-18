using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Services;

public interface ITeamService
{
    // ── Admin ────────────────────────────────────────────
    Task<IReadOnlyList<TeamDto>> ListTeamsAsync(CancellationToken ct = default);
    Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default);
    Task<TeamDto?> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto, CancellationToken ct = default);
    Task<bool> DeleteTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<TeamDto?> ToggleVerifyAsync(Guid teamId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamTitleRequestDto>> ListAllRequestsAsync(RequestStatus? status, CancellationToken ct = default);
    Task<TeamTitleRequestDto?> DecideRequestAsync(Guid requestId, bool approve, CancellationToken ct = default);

    // ── Team / member ────────────────────────────────────
    Task<TeamDetailDto?> GetTeamAsync(Guid teamId, Guid? currentUserId, CancellationToken ct = default);
    Task<IReadOnlyList<TeamDto>> GetMyTeamsAsync(Guid userId, CancellationToken ct = default);
    Task UpdateMemberRoleAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid targetUserId, TeamRole role, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid targetUserId, CancellationToken ct = default);

    // ── Invites ──────────────────────────────────────────
    Task CreateInviteAsync(Guid teamId, Guid actingUserId, bool isAdmin, string username, TeamRole role, CancellationToken ct = default);
    Task<IReadOnlyList<TeamInviteDto>> GetMyInvitesAsync(Guid userId, CancellationToken ct = default);
    Task AcceptInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default);
    Task DeclineInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default);

    // ── Title requests ───────────────────────────────────
    Task<TeamTitleRequestDto> RequestTitleAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid bookId, string? message, CancellationToken ct = default);

    // ── Authorization helpers ────────────────────────────
    Task<TeamRole?> GetUserRoleInBookTeamAsync(Guid userId, Guid bookId, CancellationToken ct = default);
    Task<bool> CanEditBookChaptersAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default);
    Task<bool> CanManageBookTitleAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default);
    Task<BookTeamDto?> GetBookTeamAsync(Guid bookId, CancellationToken ct = default);
}
