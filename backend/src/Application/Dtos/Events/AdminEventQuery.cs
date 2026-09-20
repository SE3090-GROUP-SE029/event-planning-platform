using Domain.Enums;

namespace Application.Dtos.Events;

public class AdminEventQuery
{
    public EventStatus? Status { get; set; }
    public EventType? EventType { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
