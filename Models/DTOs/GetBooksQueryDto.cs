using LibNode.Api.Models.Enums;

namespace LibNode.Api.Models.DTOs;

public record GetBooksQueryDto
{
    public Guid? Cursor { get; init; }
    public int Limit { get; init; } = 20;
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
    public BookSortBy? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public List<BookType>? Types { get; init; }
    public List<OriginalStatus>? OriginalStatuses { get; init; }
    public List<TranslationStatus>? TranslationStatuses { get; init; }
    public List<string>? Tags { get; init; }
    public List<string>? Categories { get; init; }
}
