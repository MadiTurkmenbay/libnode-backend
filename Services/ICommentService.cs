using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface ICommentService
{
    Task<CursorPagedResult<CommentDto, Guid>> GetBookCommentsAsync(Guid bookId, Guid? currentUserId, Guid? cursor, int limit, string sort, CancellationToken ct = default);
    Task<CursorPagedResult<CommentDto, Guid>> GetChapterCommentsAsync(Guid chapterId, Guid? currentUserId, Guid? cursor, int limit, string sort, CancellationToken ct = default);
    Task<CommentDto> CreateBookCommentAsync(Guid bookId, Guid userId, string content, Guid? parentId, CancellationToken ct = default);
    Task<CommentDto> CreateChapterCommentAsync(Guid chapterId, Guid userId, string content, Guid? parentId, CancellationToken ct = default);
    Task<CommentVoteResultDto> VoteAsync(Guid commentId, Guid userId, short value, CancellationToken ct = default);
    Task<bool> SetPinnedAsync(Guid commentId, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid commentId, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<CursorPagedResult<CommentDto, Guid>> GetRecentForModerationAsync(Guid? cursor, int limit, CancellationToken ct = default);

    // ── Reports ──────────────────────────────────────────
    Task ReportAsync(Guid commentId, Guid reporterUserId, string reason, CancellationToken ct = default);
    Task<CursorPagedResult<CommentReportDto, Guid>> GetReportsAsync(bool? resolved, Guid? cursor, int limit, CancellationToken ct = default);
    Task<bool> ResolveReportAsync(Guid reportId, CancellationToken ct = default);
}
