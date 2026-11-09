using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _configuration;

    public BlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        ImageUploadValidator.Validate(file);

        if (folder is not ("event-banners" or "profile-images"))
            throw new ArgumentException("Unsupported image folder.");

        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobPath = $"{folder}/{Guid.NewGuid():N}{extension}";
        var blob = container.GetBlobClient(blobPath);

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        await blob.UploadAsync(
            buffer,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            },
            cancellationToken);

        return blobPath;
    }

    public async Task<StoredImage?> DownloadAsync(
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidBlobPath(blobPath))
            return null;

        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure Blob Storage is not configured.");

        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        var blob = new BlobContainerClient(connectionString, containerName).GetBlobClient(blobPath);

        if (!(await blob.ExistsAsync(cancellationToken)).Value)
            return null;

        var result = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return new StoredImage(
            result.Value.Content,
            result.Value.Details.ContentType ?? "application/octet-stream");
    }

    private static bool IsValidBlobPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.Contains(".."))
            return false;

        var parts = path.Split('/');
        if (parts.Length != 2 || parts[0] is not ("event-banners" or "profile-images"))
            return false;

        var extension = Path.GetExtension(parts[1]).ToLowerInvariant();
        var name = Path.GetFileNameWithoutExtension(parts[1]);
        return Guid.TryParseExact(name, "N", out _) &&
            extension is ".jpg" or ".jpeg" or ".png" or ".webp";
    }
}