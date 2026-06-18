using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;

namespace LibNode.Api.Services;

/// <summary>Топ-N рейтинги книг (страница /rankings).</summary>
public interface IRankingService
{
    Task<IReadOnlyList<BookDto>> GetRankingAsync(RankingType type, int limit, CancellationToken ct = default);
}
