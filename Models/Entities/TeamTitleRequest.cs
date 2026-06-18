using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.Entities;

/// <summary>
/// Заявка команды на право переводить конкретный тайтл. Одобряет администратор;
/// при одобрении тайтл закрепляется за командой (Book.TeamId).
/// </summary>
public class TeamTitleRequest
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
}
