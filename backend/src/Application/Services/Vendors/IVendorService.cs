using Application.Dtos.Vendors;

namespace Application.Services.Vendors;

public interface IVendorService
{
    Task<VendorProfileResponse> CreateProfileAsync(Guid userId, CreateVendorProfileRequest request);
    Task<VendorProfileResponse> GetMyProfileAsync(Guid userId);
    Task<VendorProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateVendorProfileRequest request);
    Task<VendorProfileResponse> UpdateProfileImageAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength);
}
