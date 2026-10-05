namespace Application.GuestManagement;

// DTO for a single parsed guest row from the uploaded CSV.
public record GuestUploadRow(
    int RowNumber,
    string? FullName,
    string? EmailAddress,
    string? Organisation,
    string? PhoneNumber);

// Per-row validation error surfaced to the planner.
public record GuestUploadRowError(int RowNumber, string Field, string Message);

// Structured result returned to the planner after the upload attempt.
public record BulkGuestUploadResult(
    int TotalRows,
    int SuccessfulRows,
    int FailedRows,
    int DuplicateRows,
    int AlreadyRegisteredRows,
    IReadOnlyList<GuestUploadRowError> Errors,
    int UploadedGuests,
    int InvalidRows,
    int DeliveryFailedRows,
    int QueuedEmails)
{
    public int Duplicates => DuplicateRows + AlreadyRegisteredRows;
}
