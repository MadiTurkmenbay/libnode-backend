namespace LibNode.Api.Services;

/// <summary>Обработка изображений (превью) на сервере.</summary>
public interface IImageService
{
    /// <summary>
    /// Сделать уменьшенную копию (вписать в квадрат maxSize, сохранив пропорции) в формате WebP.
    /// Возвращает поток с позиции 0. Бросает, если поток не является валидным изображением.
    /// </summary>
    Task<MemoryStream> MakeThumbnailWebpAsync(Stream source, int maxSize, CancellationToken ct = default);

    /// <summary>
    /// Перекодировать изображение в WebP, попутно удаляя метаданные (EXIF/GPS/ICC/XMP) и
    /// ограничивая максимальную сторону <paramref name="maxDimension"/> (без апскейла).
    /// Валидирует, что поток действительно является изображением (по содержимому, не по Content-Type).
    /// Возвращает поток с позиции 0; бросает при невалидном изображении.
    /// </summary>
    Task<MemoryStream> ReencodeWebpAsync(Stream source, int maxDimension, CancellationToken ct = default);
}
