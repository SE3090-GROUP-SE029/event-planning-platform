using Application.Common.Interfaces;

namespace Infrastructure.Storage;

public class LocalVendorImageStorage : IVendorImageStorage
{
    public const long MaxFileBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly string _storageDirectoryPath;

    public LocalVendorImageStorage(string storageDirectoryPath)
    {
        _storageDirectoryPath = storageDirectoryPath;
    }

    public async Task<string> SaveAsync(
        Guid vendorId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        if (content is null || contentLength <= 0)
        {
            throw new ArgumentException("An image file is required.");
        }

        if (contentLength > MaxFileBytes)
        {
            throw new ArgumentException("Image must be 2 MB or smaller.");
        }

        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException("Image must be a JPEG, PNG, or WebP file.");
        }

        var extension = ExtensionByContentType[contentType];
        await using var imageBytes = await ReadImageAsync(content, contentType, cancellationToken);
        if (imageBytes.Length != contentLength)
        {
            throw new ArgumentException("The uploaded image size could not be verified.");
        }
        Directory.CreateDirectory(_storageDirectoryPath);

        var fileName = $"{vendorId:N}_{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_storageDirectoryPath, fileName);

        await using (var stream = File.Create(physicalPath))
        {
            imageBytes.Position = 0;
            await imageBytes.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/vendors/{fileName}";
    }

    private static async Task<MemoryStream> ReadImageAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        var imageBytes = new MemoryStream();
        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }
            if (imageBytes.Length + bytesRead > MaxFileBytes)
            {
                await imageBytes.DisposeAsync();
                throw new ArgumentException("Image must be 2 MB or smaller.");
            }

            await imageBytes.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (!HasMatchingSignature(imageBytes.GetBuffer().AsSpan(0, (int)imageBytes.Length), contentType))
        {
            await imageBytes.DisposeAsync();
            throw new ArgumentException("The uploaded file is not a valid JPEG, PNG, or WebP image.");
        }

        return imageBytes;
    }

    private static bool HasMatchingSignature(ReadOnlySpan<byte> bytes, string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" =>
                bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/png" =>
                bytes.Length >= 8
                && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" =>
                bytes.Length >= 12
                && bytes[..4].SequenceEqual("RIFF"u8)
                && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
    }

    public Task DeleteIfExistsAsync(string? relativeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)
            || !relativeUrl.StartsWith("/uploads/vendors/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var fileName = relativeUrl["/uploads/vendors/".Length..];
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
        {
            return Task.CompletedTask;
        }

        var physicalPath = Path.Combine(_storageDirectoryPath, fileName);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }
}
