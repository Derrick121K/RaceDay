using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services;

public static class ImageUploadValidator
{
    public const long MaximumFileSize = 5 * 1024 * 1024;

    public static void Validate(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
            throw new ArgumentException("Choose an image file.");

        if (file.Length > MaximumFileSize)
            throw new ArgumentException("Images must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();

        var expectedType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (expectedType is null || contentType != expectedType)
            throw new ArgumentException("Only JPEG, PNG and WebP images are accepted.");

        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = 0;
        while (read < header.Length)
        {
            var count = stream.Read(header, read, header.Length - read);
            if (count == 0) break;
            read += count;
        }

        var valid = extension switch
        {
            ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => read >= 3 &&
                header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => read >= 12 &&
                System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            _ => false
        };

        if (!valid)
            throw new ArgumentException("The file contents do not match the selected image type.");
    }
}