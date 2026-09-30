using Domain.Enums;

namespace Application.Dtos.Vendors;

public class AdminVendorQuery
{
    public VendorStatus? Status { get; set; }
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
