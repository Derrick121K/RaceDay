using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public sealed class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> _images = new();

    public async Task<string> UploadAsync(
        IFormFile file,
        string folder,
        CancellationToken cancellationToken = default)
    {
        ImageUploadValidator.Validate(file);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var path = $"{folder}/{Guid.NewGuid():N}{extension}";

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        _images[path] = (buffer.ToArray(), file.ContentType);
        return path;
    }

    public Task<StoredImage?> DownloadAsync(
        string blobPath,
        CancellationToken cancellationToken = default)
    {
        if (!_images.TryGetValue(blobPath, out var image))
            return Task.FromResult<StoredImage?>(null);

        StoredImage stored = new(new MemoryStream(image.Data, writable: false), image.ContentType);
        return Task.FromResult<StoredImage?>(stored);
    }
}