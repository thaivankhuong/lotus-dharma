namespace LotusDharma.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<BlobDownloadResult?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string containerName,
        string blobName,
        long offset = 0,
        long? length = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(
        string containerName,
        string blobName,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}

public class BlobDownloadResult
{
    public Stream Content { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string? ETag { get; set; }
}
