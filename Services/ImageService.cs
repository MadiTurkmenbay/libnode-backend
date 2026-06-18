using LibNode.Api.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Processing;

namespace LibNode.Api.Services;

public class ImageService : IImageService
{
    // Бюджет пикселей: защита от decompression-bomb (маленький файл → гигантский bitmap в памяти).
    private const long MaxPixels = 50L * 1_000_000; // 50 мегапикселей

    static ImageService()
    {
        // Жёсткий потолок на аллокации ImageSharp (защита от decompression-bomb / OOM).
        Configuration.Default.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
        {
            AllocationLimitMegabytes = 256,
        });
    }

    public async Task<MemoryStream> MakeThumbnailWebpAsync(Stream source, int maxSize, CancellationToken ct = default)
    {
        using var buffer = await GuardAndBufferAsync(source, ct);

        using var image = await Image.LoadAsync(buffer, ct);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max, // вписать в maxSize×maxSize, сохранив пропорции, без апскейла сверх исходного
            Size = new Size(maxSize, maxSize),
        }));

        var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 80 }, ct);
        output.Position = 0;
        return output;
    }

    public async Task<MemoryStream> ReencodeWebpAsync(Stream source, int maxDimension, CancellationToken ct = default)
    {
        using var buffer = await GuardAndBufferAsync(source, ct);

        using var image = await Image.LoadAsync(buffer, ct);

        if (image.Width > maxDimension || image.Height > maxDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension),
            }));
        }

        // Удаляем метаданные (приватность + размер): EXIF/GPS, IPTC, XMP, ICC.
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IccProfile = null;

        var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 85 }, ct);
        output.Position = 0;
        return output;
    }

    /// <summary>
    /// Буферизует поток в перематываемый <see cref="MemoryStream"/>, выполняет дешёвый
    /// <c>Identify</c> по заголовку и отклоняет изображения, превышающие бюджет пикселей,
    /// ДО полного декодирования (защита от decompression-bomb). Возвращает поток с позиции 0.
    /// </summary>
    private static async Task<MemoryStream> GuardAndBufferAsync(Stream source, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var info = await Image.IdentifyAsync(buffer, ct);
        buffer.Position = 0;

        var pixels = (long)info.Width * info.Height;
        if (pixels > MaxPixels)
            throw new ImageTooLargeException(
                $"Слишком большое изображение ({info.Width}×{info.Height}). Максимум {MaxPixels / 1_000_000} мегапикселей.");

        return buffer;
    }
}
