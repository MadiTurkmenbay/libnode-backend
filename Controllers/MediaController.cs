using System.Security.Claims;
using LibNode.Api.Data;
using LibNode.Api.Exceptions;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace LibNode.Api.Controllers;

/// <summary>Загрузка изображений в объектное хранилище (MinIO): аватары, обложки.</summary>
[ApiController]
[Authorize]
public class MediaController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 МБ
    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
        ["image/gif"] = "gif",
    };

    private readonly IStorageService _storage;
    private readonly IImageService _images;
    private readonly AppDbContext _db;
    private readonly ITeamService _teams;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IStorageService storage, IImageService images, AppDbContext db, ITeamService teams, ILogger<MediaController> logger)
    {
        _storage = storage;
        _images = images;
        _db = db;
        _teams = teams;
        _logger = logger;
    }

    /// <summary>
    /// Перекодировать оригинал в WebP (валидация по содержимому + удаление EXIF), загрузить его и
    /// WebP-превью. Бросает исключение ImageSharp, если файл не является корректным изображением
    /// (ловится в контроллере → 400).
    /// </summary>
    private async Task<(string Full, string? Thumb)> UploadWithThumbAsync(
        IFormFile file, string prefix, int fullMax, int thumbSize, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        // Оригинал: перекодируем в WebP по реальному содержимому (magic-bytes валидирует ImageSharp).
        buffer.Position = 0;
        await using var fullStream = await _images.ReencodeWebpAsync(buffer, fullMax, ct);
        var full = await _storage.UploadAsync(fullStream, "image/webp", prefix, "webp", ct);

        string? thumb = null;
        try
        {
            buffer.Position = 0;
            await using var thumbStream = await _images.MakeThumbnailWebpAsync(buffer, thumbSize, ct);
            thumb = await _storage.UploadAsync(thumbStream, "image/webp", $"{prefix}/thumbs", "webp", ct);
        }
        catch (Exception ex)
        {
            // Превью — не критично: если генерация не удалась, оставляем только оригинал.
            _logger.LogWarning(ex, "Thumbnail generation failed for prefix {Prefix}", prefix);
        }
        return (full, thumb);
    }

    private Guid GetUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    private ActionResult? ValidateFile(IFormFile? file)
    {
        if (!_storage.IsConfigured)
            return StatusCode(503, new { error = "Хранилище изображений не настроено." });
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Файл не передан." });
        if (file.Length > MaxBytes)
            return BadRequest(new { error = "Файл слишком большой (макс. 5 МБ)." });
        // Тип content-type — мягкий пред-фильтр; реальная валидация по содержимому в ReencodeWebpAsync.
        if (!AllowedTypes.ContainsKey(file.ContentType))
            return BadRequest(new { error = "Поддерживаются только JPG, PNG, WEBP, GIF." });
        return null;
    }

    /// <summary>Публичный флаг доступности хранилища (для graceful-degrade на фронте).</summary>
    [HttpGet("api/media/config")]
    [AllowAnonymous]
    public IActionResult Config() => Ok(new { configured = _storage.IsConfigured });

    /// <summary>Статистика хранилища для админки (число объектов + суммарный размер).</summary>
    [HttpGet("api/admin/storage")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StorageStats(CancellationToken ct)
    {
        if (!_storage.IsConfigured)
            return Ok(new { configured = false, objectCount = 0L, totalBytes = 0L });
        var (count, bytes) = await _storage.GetStatsAsync(ct);
        return Ok(new { configured = true, objectCount = count, totalBytes = bytes });
    }

    /// <summary>Универсальная загрузка изображения (для будущих галерей/контента). Лимит по пользователю.</summary>
    [HttpPost("api/media")]
    [EnableRateLimiting("media")]
    public async Task<IActionResult> UploadMedia(IFormFile? file, CancellationToken ct)
    {
        var error = ValidateFile(file);
        if (error != null) return error;

        (string url, string? thumb) result;
        try { result = await UploadWithThumbAsync(file!, "uploads", 1600, 300, ct); }
        catch (UnknownImageFormatException) { return BadRequest(new { error = "Файл не является корректным изображением." }); }
        catch (InvalidImageContentException) { return BadRequest(new { error = "Файл повреждён или не является изображением." }); }
        catch (ImageTooLargeException ex) { return BadRequest(new { error = ex.Message }); }

        return Ok(new { url = result.url, thumbUrl = result.thumb });
    }

    [HttpPost("api/me/avatar")]
    [EnableRateLimiting("media")]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken ct)
    {
        var error = ValidateFile(file);
        if (error != null) return error;

        var userId = GetUserId();
        (string url, string? thumb) result;
        try { result = await UploadWithThumbAsync(file!, "avatars", 512, 128, ct); }
        catch (UnknownImageFormatException) { return BadRequest(new { error = "Файл не является корректным изображением." }); }
        catch (InvalidImageContentException) { return BadRequest(new { error = "Файл повреждён или не является изображением." }); }
        catch (ImageTooLargeException ex) { return BadRequest(new { error = ex.Message }); }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return NotFound();
        var (oldUrl, oldThumb) = (user.AvatarUrl, user.AvatarThumbUrl);
        user.AvatarUrl = result.url;
        user.AvatarThumbUrl = result.thumb;
        await _db.SaveChangesAsync(ct);

        // Чистим прежние объекты (best-effort, no-op для внешних URL).
        await _storage.DeleteAsync(oldUrl, ct);
        await _storage.DeleteAsync(oldThumb, ct);

        return Ok(new { avatarUrl = result.url, avatarThumbUrl = result.thumb });
    }

    [HttpPost("api/admin/books/{bookId:guid}/cover")]
    [EnableRateLimiting("media")]
    public async Task<IActionResult> UploadCover(Guid bookId, IFormFile? file, CancellationToken ct)
    {
        var error = ValidateFile(file);
        if (error != null) return error;

        var userId = GetUserId();
        if (!await _teams.CanManageBookTitleAsync(userId, bookId, User.IsInRole("Admin"), ct))
            return Forbid();

        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book == null) return NotFound();

        (string url, string? thumb) result;
        try { result = await UploadWithThumbAsync(file!, "covers", 1200, 300, ct); }
        catch (UnknownImageFormatException) { return BadRequest(new { error = "Файл не является корректным изображением." }); }
        catch (InvalidImageContentException) { return BadRequest(new { error = "Файл повреждён или не является изображением." }); }
        catch (ImageTooLargeException ex) { return BadRequest(new { error = ex.Message }); }

        var (oldUrl, oldThumb) = (book.CoverUrl, book.CoverThumbUrl);
        book.CoverUrl = result.url;
        book.CoverThumbUrl = result.thumb;
        await _db.SaveChangesAsync(ct);

        await _storage.DeleteAsync(oldUrl, ct);
        await _storage.DeleteAsync(oldThumb, ct);

        return Ok(new { coverUrl = result.url, coverThumbUrl = result.thumb });
    }
}
