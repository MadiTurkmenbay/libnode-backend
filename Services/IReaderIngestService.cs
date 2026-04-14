using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface IReaderIngestService
{
    Task<(ReaderResourceDto Resource, bool Created)> CreateOrGetTitleAsync(
        ReaderTitlePayloadDto dto,
        CancellationToken ct = default);

    Task<(ReaderResourceDto Resource, bool Created)> CreateOrUpdateChapterAsync(
        ReaderChapterPayloadDto dto,
        CancellationToken ct = default);
}
