namespace LibNode.Api.Services;

/// <summary>Объектное хранилище (MinIO/S3) для изображений: обложки, аватары и т.п.</summary>
public interface IStorageService
{
    /// <summary>Доступно ли хранилище (настроены ли Storage:* параметры).</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Загрузить файл и вернуть его публичный URL.
    /// <paramref name="keyPrefix"/> — папка-префикс ключа (например "avatars" или "covers").
    /// </summary>
    Task<string> UploadAsync(Stream content, string contentType, string keyPrefix, string fileExtension, CancellationToken ct = default);

    /// <summary>
    /// Best-effort удаление объекта по его публичному URL (для очистки заменённых обложек/аватаров).
    /// No-op, если URL пустой, внешний (не из нашего хранилища) или хранилище не настроено; ошибки гасятся.
    /// </summary>
    Task DeleteAsync(string? publicUrl, CancellationToken ct = default);

    /// <summary>Статистика бакета: число объектов и суммарный размер в байтах (для админки).</summary>
    Task<(long ObjectCount, long TotalBytes)> GetStatsAsync(CancellationToken ct = default);
}
