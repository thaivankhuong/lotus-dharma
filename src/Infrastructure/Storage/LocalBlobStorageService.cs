using LotusDharma.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.Storage;

public class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalBlobStorageService> _logger;

    public LocalBlobStorageService(IConfiguration configuration, ILogger<LocalBlobStorageService> logger)
    {
        _basePath = configuration["BlobStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string containerName, string blobName, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_basePath, containerName);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, blobName);
        using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("Uploaded blob: {Container}/{Blob}", containerName, blobName);
        return $"/uploads/{containerName}/{blobName}";
    }

    public Task<BlobDownloadResult?> DownloadAsync(
        string containerName, string blobName,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, containerName, blobName);
        if (!File.Exists(filePath))
            return Task.FromResult<BlobDownloadResult?>(null);

        var fileInfo = new FileInfo(filePath);
        var stream = File.OpenRead(filePath);

        return Task.FromResult<BlobDownloadResult?>(new BlobDownloadResult
        {
            Content = stream,
            ContentType = GetContentType(blobName),
            ContentLength = fileInfo.Length
        });
    }

    public Task<Stream?> OpenReadAsync(
        string containerName, string blobName,
        long offset = 0, long? length = null,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, containerName, blobName);
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        var stream = File.OpenRead(filePath);
        if (offset > 0)
            stream.Seek(offset, SeekOrigin.Begin);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string containerName, string blobName,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, containerName, blobName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted blob: {Container}/{Blob}", containerName, blobName);
        }
        return Task.CompletedTask;
    }

    public Task<string> GetPresignedUrlAsync(
        string containerName, string blobName, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"/uploads/{containerName}/{blobName}");
    }

    public Task<bool> ExistsAsync(
        string containerName, string blobName,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, containerName, blobName);
        return Task.FromResult(File.Exists(filePath));
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".flac" => "audio/flac",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
