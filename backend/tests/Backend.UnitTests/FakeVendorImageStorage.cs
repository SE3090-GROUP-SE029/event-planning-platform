using Application.Common.Interfaces;

namespace Backend.UnitTests;

internal sealed class FakeVendorImageStorage : IVendorImageStorage
{
    public string? LastSavedUrl { get; private set; }

    public Task<string> SaveAsync(
        Guid vendorId,
        Stream content,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        LastSavedUrl = $"/uploads/vendors/{vendorId:N}.jpg";
        return Task.FromResult(LastSavedUrl);
    }

    public Task DeleteIfExistsAsync(string? relativeUrl, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
