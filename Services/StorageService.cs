using Amazon.S3;
using Amazon.S3.Model;

namespace LibNode.Api.Services;

/// <summary>
/// Реализация объектного хранилища поверх S3-совместимого MinIO (AWSSDK.S3).
/// Конфигурация в секции Storage: Endpoint, AccessKey, SecretKey, Bucket, PublicUrl.
/// </summary>
public class StorageService : IStorageService
{
    private readonly IAmazonS3? _s3;
    private readonly string _bucket;
    private readonly string _publicUrl;
    private readonly ILogger<StorageService> _logger;

    public StorageService(IConfiguration config, ILogger<StorageService> logger)
    {
        _logger = logger;
        var section = config.GetSection("Storage");
        var endpoint = section["Endpoint"];
        var accessKey = section["AccessKey"];
        var secretKey = section["SecretKey"];
        _bucket = section["Bucket"] ?? "libnode";
        _publicUrl = (section["PublicUrl"] ?? string.Empty).TrimEnd('/');

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("Storage (MinIO) is not configured — uploads disabled.");
            return;
        }

        _s3 = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true, // MinIO требует path-style адресацию бакета
            AuthenticationRegion = "us-east-1",
        });
    }

    public bool IsConfigured => _s3 != null;

    public async Task<string> UploadAsync(Stream content, string contentType, string keyPrefix, string fileExtension, CancellationToken ct = default)
    {
        if (_s3 == null)
            throw new InvalidOperationException("Хранилище не настроено.");

        var ext = fileExtension.TrimStart('.').ToLowerInvariant();
        var key = $"{keyPrefix.Trim('/')}/{Guid.CreateVersion7():n}.{ext}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            // НЕ ставим DisablePayloadSigning: по HTTP (внутренний endpoint) это запрещено SDK.
            // Длина потока известна (IFormFile) → обычной подписи достаточно для MinIO.
            UseChunkEncoding = false,
            // Не закрывать переданный поток — вызывающий код может переиспользовать буфер (превью).
            AutoCloseStream = false,
        }, ct);

        return $"{_publicUrl}/{_bucket}/{key}";
    }

    public async Task DeleteAsync(string? publicUrl, CancellationToken ct = default)
    {
        if (_s3 == null || string.IsNullOrWhiteSpace(publicUrl)) return;

        // Удаляем только объекты ИЗ нашего хранилища (по совпадению публичного префикса).
        var prefix = $"{_publicUrl}/{_bucket}/";
        if (!publicUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return;

        var key = publicUrl[prefix.Length..];
        if (string.IsNullOrWhiteSpace(key)) return;

        try
        {
            await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete orphaned object {Key}", key);
        }
    }

    public async Task<(long ObjectCount, long TotalBytes)> GetStatsAsync(CancellationToken ct = default)
    {
        if (_s3 == null) return (0, 0);

        long count = 0;
        long bytes = 0;
        string? token = null;
        do
        {
            var resp = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                ContinuationToken = token,
                MaxKeys = 1000,
            }, ct);
            foreach (var o in resp.S3Objects)
            {
                count++;
                bytes += o.Size;
            }
            token = resp.IsTruncated == true ? resp.NextContinuationToken : null;
        }
        while (token != null);

        return (count, bytes);
    }
}
