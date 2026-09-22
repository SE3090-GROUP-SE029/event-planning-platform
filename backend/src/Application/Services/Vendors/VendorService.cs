using System.Net.Mail;
using Application.Common.Interfaces;
using Application.Dtos.Vendors;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Vendors;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendors;

    public VendorService(IVendorRepository vendors)
    {
        _vendors = vendors;
    }

    public async Task<VendorProfileResponse> CreateProfileAsync(Guid userId, CreateVendorProfileRequest request)
    {
        var existing = await _vendors.GetByUserIdAsync(userId);
        if (existing is not null)
        {
            throw new InvalidOperationException("A vendor profile already exists for this account.");
        }

        var category = ParseCategory(request.Category);
        ValidateProfileFields(request.BusinessName, request.ContactEmail, request.ContactPhone, request.Description);

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = request.BusinessName.Trim(),
            Category = category,
            ContactEmail = request.ContactEmail.Trim().ToLowerInvariant(),
            ContactPhone = request.ContactPhone.Trim(),
            Description = NormalizeDescription(request.Description),
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
        ValidateProfileFields(request.BusinessName, request.ContactEmail, request.ContactPhone, request.Description);

        vendor.BusinessName = request.BusinessName.Trim();
        vendor.Category = category;
        vendor.ContactEmail = request.ContactEmail.Trim().ToLowerInvariant();
        vendor.ContactPhone = request.ContactPhone.Trim();
        vendor.Description = NormalizeDescription(request.Description);
        vendor.UpdatedAt = DateTime.UtcNow;

        await _vendors.SaveChangesAsync();
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

    private static void ValidateProfileFields(string? businessName, string? contactEmail, string? contactPhone, string? description)
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

        if (description is not null && description.Trim().Length > 2000)
        {
            throw new ArgumentException("Description must be 2000 characters or fewer.");
        }
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
        Description = vendor.Description,
        Status = vendor.Status.ToString(),
        CreatedAt = vendor.CreatedAt,
        UpdatedAt = vendor.UpdatedAt
    };
}
