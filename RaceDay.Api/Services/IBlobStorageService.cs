using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services;

public sealed record StoredImage(Stream Content, string ContentType);

public interface IBlobStorageService
{
    Task<string> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<StoredImage?> DownloadAsync(string blobPath, CancellationToken cancellationToken = default);
}