namespace Application.Dtos.Admin;

public sealed class AdminScheduleQuery
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class AdminScheduleListResponse
{
    public IReadOnlyList<AdminScheduleListItemResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class AdminScheduleListItemResponse
{
    public Guid ScheduleId { get; init; }
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public TimeOnly EventStartTime { get; init; }
    public TimeOnly EventEndTime { get; init; }
    public int ActivityCount { get; init; }
    public int UnresolvedConflictCount { get; init; }
}

public sealed class AdminScheduleDetailResponse
{
    public Guid ScheduleId { get; init; }
    public AdminScheduleEventResponse Event { get; init; } = new();
    public IReadOnlyList<AdminScheduleActivityResponse> Activities { get; init; } = [];
    public IReadOnlyList<AdminScheduleConflictResponse> Conflicts { get; init; } = [];
    public int ActivityCount => Activities.Count;
    public int UnresolvedConflictCount => Conflicts.Count(conflict => !conflict.IsResolved);
    public int ResolvedConflictCount => Conflicts.Count(conflict => conflict.IsResolved);
}

public sealed class AdminScheduleEventResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public TimeOnly EventStartTime { get; init; }
    public TimeOnly EventEndTime { get; init; }
    public string EventStatus { get; init; } = string.Empty;
    public AdminSchedulePlannerResponse? Planner { get; init; }
}

public sealed class AdminSchedulePlannerResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

public sealed class AdminScheduleActivityResponse
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public AdminScheduleVendorResponse? AssignedVendor { get; init; }
}

public sealed class AdminScheduleVendorResponse
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public sealed class AdminScheduleConflictResponse
{
    public Guid Id { get; init; }
    public Guid ActivityId1 { get; init; }
    public Guid ActivityId2 { get; init; }
    public string ConflictType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
    public DateTime DetectedAt { get; init; }
}
