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

    private readonly string _webRootPath;

    public LocalVendorImageStorage(string webRootPath)
    {
        _webRootPath = webRootPath;
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

        Directory.CreateDirectory(_webRootPath);
        var folder = Path.Combine(_webRootPath, "uploads", "vendors");
        Directory.CreateDirectory(folder);

        var extension = ExtensionByContentType[contentType];
        var fileName = $"{vendorId:N}_{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(folder, fileName);

        await using var stream = File.Create(physicalPath);
        await content.CopyToAsync(stream, cancellationToken);

        return $"/uploads/vendors/{fileName}";
    }

    public Task DeleteIfExistsAsync(string? relativeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)
            || !relativeUrl.StartsWith("/uploads/vendors/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(_webRootPath, relativePath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }
}
