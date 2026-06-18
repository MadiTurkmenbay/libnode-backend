using LibNode.Api.Services;

namespace LibNode.Api.Tests.Unit;

/// <summary>
/// Тестовый двойник <see cref="IStorageService"/>: ведёт себя как ненастроенное хранилище.
/// ResolveUrl возвращает ключ без изменений (как и реальный StorageService при IsConfigured == false),
/// поэтому юнит-тесты сравнивают сырые ключи/значения без склейки публичного URL.
/// </summary>
internal sealed class FakeStorageService : IStorageService
{
    public bool IsConfigured => false;

    public string PublicBase => string.Empty;

    public Task<string> UploadAsync(Stream content, string contentType, string keyPrefix, string fileExtension, CancellationToken ct = default)
        => Task.FromResult($"{keyPrefix.Trim('/')}/{Guid.NewGuid():n}.{fileExtension.TrimStart('.')}");

    public string ResolveUrl(string? key) => key ?? string.Empty;

    public Task DeleteAsync(string? key, CancellationToken ct = default) => Task.CompletedTask;

    public Task<(long ObjectCount, long TotalBytes)> GetStatsAsync(CancellationToken ct = default)
        => Task.FromResult((0L, 0L));
}
