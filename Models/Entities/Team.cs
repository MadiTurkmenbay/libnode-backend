namespace LibNode.Api.Models.Entities;

/// <summary>
/// Команда переводчиков. Может владеть несколькими тайтлами; у тайтла — максимум одна команда.
/// </summary>
public class Team
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Slug { get; set; }

    public string? Description { get; set; }

    public bool IsVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<Book> Books { get; set; } = new List<Book>();
    public ICollection<TeamTitleRequest> Requests { get; set; } = new List<TeamTitleRequest>();
}
