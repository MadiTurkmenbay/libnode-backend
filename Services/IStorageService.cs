namespace LibNode.Api.Services;

/// <summary>Объектное хранилище (MinIO/S3) для изображений: обложки, аватары и т.п.</summary>
public interface IStorageService
{
    /// <summary>Доступно ли хранилище (настроены ли Storage:* параметры).</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Публичный префикс ("{PublicUrl}/{Bucket}/") для склейки ключа объекта в абсолютный URL.
    /// Пустая строка, если хранилище не настроено. Пригоден как захваченная локальная константа
    /// внутри EF LINQ-проекций (EF транслирует конкатенацию строк).
    /// </summary>
    string PublicBase { get; }

    /// <summary>
    /// Загрузить файл и вернуть КЛЮЧ объекта (например "avatars/&lt;guid&gt;.webp"), а НЕ абсолютный URL.
    /// Ключ — это то, что хранится в БД; абсолютный URL собирается на чтении через <see cref="ResolveUrl"/>.
    /// <paramref name="keyPrefix"/> — папка-префикс ключа (например "avatars" или "covers").
    /// </summary>
    Task<string> UploadAsync(Stream content, string contentType, string keyPrefix, string fileExtension, CancellationToken ct = default);

    /// <summary>
    /// Собрать абсолютный публичный URL из ключа объекта. Если ключ пуст — возвращает пустую строку.
    /// Если ключ уже абсолютный (начинается с http) — возвращает как есть (внешние/дефолтные аватары).
    /// Если хранилище не настроено — возвращает ключ без изменений.
    /// </summary>
    string ResolveUrl(string? key);

    /// <summary>
    /// Best-effort удаление объекта по его КЛЮЧУ (для очистки заменённых обложек/аватаров).
    /// Если передан легаси-абсолютный URL из нашего хранилища — префикс <see cref="PublicBase"/> срезается.
    /// No-op, если ключ пустой, внешний (не из нашего хранилища) или хранилище не настроено; ошибки гасятся.
    /// </summary>
    Task DeleteAsync(string? key, CancellationToken ct = default);

    /// <summary>Статистика бакета: число объектов и суммарный размер в байтах (для админки).</summary>
    Task<(long ObjectCount, long TotalBytes)> GetStatsAsync(CancellationToken ct = default);
}
