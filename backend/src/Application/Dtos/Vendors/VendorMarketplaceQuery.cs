namespace Application.Dtos.Vendors;

public class VendorMarketplaceQuery
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? SortBy { get; set; } = "businessName";
    public string? SortOrder { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
