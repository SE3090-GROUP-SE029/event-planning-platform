namespace Application.Common.Interfaces;

public interface IVendorImageStorage
{
    Task<string> SaveAsync(
        Guid vendorId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(string? relativeUrl, CancellationToken cancellationToken = default);
}
