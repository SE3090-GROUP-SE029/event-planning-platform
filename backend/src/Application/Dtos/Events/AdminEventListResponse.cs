namespace Application.Dtos.Events;

public class AdminEventListResponse
{
    public IReadOnlyList<AdminEventResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
