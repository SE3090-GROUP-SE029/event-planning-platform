namespace Application.DTOs.Scheduling;

public record CreateActivityRequest(
    string Title,
    string? Description,
    DateTime StartTime,
    DateTime EndTime,
    Guid? AssignedVendorId
);