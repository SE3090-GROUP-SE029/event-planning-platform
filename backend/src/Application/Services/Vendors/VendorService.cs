using System.Net.Mail;
using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Vendors;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendors;
    private readonly IVendorImageStorage _images;

    public VendorService(IVendorRepository vendors, IVendorImageStorage images)
    {
        _vendors = vendors;
        _images = images;
    }

    public async Task<VendorProfileResponse> CreateProfileAsync(Guid userId, CreateVendorProfileRequest request)
    {
        var existing = await _vendors.GetByUserIdAsync(userId);
        if (existing is not null)
        {
            throw new InvalidOperationException("A vendor profile already exists for this account.");
        }

        var category = ParseCategory(request.Category);
        var websiteUrl = NormalizeWebsiteUrl(request.WebsiteUrl);
        ValidateProfileFields(
            request.BusinessName,
            request.ContactEmail,
            request.ContactPhone,
            request.Address,
            request.Description);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = request.BusinessName.Trim(),
            Category = category,
            ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
            ContactPhone = request.ContactPhone.Trim(),
            Address = request.Address.Trim(),
            Description = NormalizeDescription(request.Description),
            WebsiteUrl = websiteUrl,
            Status = VendorStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        await _vendors.AddAsync(vendor);
        await _vendors.SaveChangesAsync();
        return ToResponse(vendor);
    }

    public async Task<VendorProfileResponse> GetMyProfileAsync(Guid userId)
    {
        var vendor = await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Vendor profile not found.");
        return ToResponse(vendor);
    }

    public async Task<VendorProfileResponse> UpdateMyProfileAsync(Guid userId, UpdateVendorProfileRequest request)
    {
        var vendor = await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Vendor profile not found.");

        var category = ParseCategory(request.Category);
        var websiteUrl = NormalizeWebsiteUrl(request.WebsiteUrl);
        ValidateProfileFields(
            request.BusinessName,
            request.ContactEmail,
            request.ContactPhone,
            request.Address,
            request.Description);

        vendor.BusinessName = request.BusinessName.Trim();
        vendor.Category = category;
        vendor.ContactEmail = request.ContactEmail.Trim().ToLowerInvariant();
        vendor.ContactPhone = request.ContactPhone.Trim();
        vendor.Address = request.Address.Trim();
        vendor.Description = NormalizeDescription(request.Description);
        vendor.WebsiteUrl = websiteUrl;
        vendor.UpdatedAt = DateTime.UtcNow;

        await _vendors.SaveChangesAsync();
        return ToResponse(vendor);
    }

    public async Task<VendorProfileResponse> UpdateProfileImageAsync(
        Guid userId,
        Stream content,
        string contentType,
        long contentLength)
    {
        var vendor = await _vendors.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Create a vendor profile before uploading an image.");

        var previousUrl = vendor.ProfileImageUrl;
        var newUrl = await _images.SaveAsync(vendor.Id, content, contentType, contentLength);
        vendor.ProfileImageUrl = newUrl;
        vendor.UpdatedAt = DateTime.UtcNow;
        await _vendors.SaveChangesAsync();

        await _images.DeleteIfExistsAsync(previousUrl);
        return ToResponse(vendor);
    }

    private static BusinessCategory ParseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category) || !Enum.TryParse<BusinessCategory>(category.Trim(), true, out var parsed))
        {
            throw new ArgumentException("Category must be one of: CATERING, PHOTOGRAPHY, VENUE, MUSIC, FLORIST, TRANSPORTATION.");
        }

        return parsed;
    }

    private static void ValidateProfileFields(
        string? businessName,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(businessName))
        {
            throw new ArgumentException("Business name is required.");
        }

        if (businessName.Trim().Length > 200)
        {
            throw new ArgumentException("Business name must be 200 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(contactEmail) || !IsValidEmail(contactEmail))
        {
            throw new ArgumentException("A valid contact email is required.");
        }

        if (string.IsNullOrWhiteSpace(contactPhone))
        {
            throw new ArgumentException("Contact phone is required.");
        }

        if (contactPhone.Trim().Length > 50)
        {
            throw new ArgumentException("Contact phone must be 50 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address is required.");
        }

        if (address.Trim().Length > 500)
        {
            throw new ArgumentException("Address must be 500 characters or fewer.");
        }

        if (description is not null && description.Trim().Length > 2000)
        {
            throw new ArgumentException("Description must be 2000 characters or fewer.");
        }
    }

    private static string? NormalizeWebsiteUrl(string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            return null;
        }

        var trimmed = websiteUrl.Trim();
        if (trimmed.Length > 500)
        {
            throw new ArgumentException("Website URL must be 500 characters or fewer.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Website URL must be a valid http or https link.");
        }

        return trimmed;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email.Trim());
            return email.Contains('@');
        }
        catch
        {
            return false;
        }
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static VendorProfileResponse ToResponse(Vendor vendor) => new()
    {
        Id = vendor.Id,
        UserId = vendor.UserId,
        BusinessName = vendor.BusinessName,
        Category = vendor.Category.ToString(),
        ContactEmail = vendor.ContactEmail,
        ContactPhone = vendor.ContactPhone,
        Address = vendor.Address,
        Description = vendor.Description,
        ProfileImageUrl = vendor.ProfileImageUrl,
        WebsiteUrl = vendor.WebsiteUrl,
        Status = vendor.Status.ToString(),
        CreatedAt = vendor.CreatedAt,
        UpdatedAt = vendor.UpdatedAt
    };
}
