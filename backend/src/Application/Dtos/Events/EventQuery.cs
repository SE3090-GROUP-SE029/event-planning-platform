using Domain.Enums;

namespace Application.Dtos.Events;

public class EventQuery
{
    public EventStatus? Status { get; set; }
    public EventType? EventType { get; set; }
    public string? SortBy { get; set; } = "createdAt";
    public string? SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
