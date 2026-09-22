namespace Application.Dtos.Events;

public class EventListResponse
{
    public IReadOnlyList<EventResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
