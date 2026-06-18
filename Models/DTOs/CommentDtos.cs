using System.ComponentModel.DataAnnotations;

namespace LibNode.Api.Models.DTOs;

/// <summary>DTO комментария для отдачи клиенту.</summary>
public record CommentDto(
    Guid Id,
    Guid BookId,
    Guid? ChapterId,
    Guid? ParentId,
    Guid UserId,
    string Username,
    string Content,
    int Score,
    int MyVote,
    bool IsPinned,
    bool IsOwn,
    int ReplyCount,
    DateTime CreatedAt,
    IReadOnlyList<CommentDto> Replies
);

/// <summary>DTO создания комментария или ответа. ParentId задаёт ответ на комментарий.</summary>
public record CreateCommentDto(
    [Required, StringLength(2000, MinimumLength = 1)]
    string Content,
    Guid? ParentId
);

/// <summary>Голос за комментарий: 1 — лайк, -1 — дизлайк, 0 — снять голос.</summary>
public record VoteCommentDto(
    [Range(-1, 1)]
    short Value
);

/// <summary>Результат голосования.</summary>
public record CommentVoteResultDto(
    Guid CommentId,
    int Score,
    int MyVote
);
