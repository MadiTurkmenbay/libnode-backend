using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

/// <summary>Таблицы лидеров (геймификация). Только публичные поля.</summary>
public interface ILeaderboardService
{
    Task<LeaderboardDto> GetAsync(int limit, CancellationToken ct = default);
}
