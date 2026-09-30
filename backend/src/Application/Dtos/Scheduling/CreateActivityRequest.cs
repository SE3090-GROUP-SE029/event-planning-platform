namespace Application.DTOs.Scheduling;

public record CreateActivityRequest(
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    Guid? AssignedVendorId
);

public record VendorScheduleActivityResponse(
    Guid Id,
    Guid ScheduleId,
    Guid EventId,
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    string Status
);
