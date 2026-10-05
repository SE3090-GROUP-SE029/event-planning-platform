using Domain.Enums;

namespace Domain.Entities;

public class GuestCheckIn
{
    public long RegistrationSubmissionId { get; set; }
    public RegistrationSubmission RegistrationSubmission { get; set; } = null!;
    public DateTimeOffset CheckedInAt { get; set; }
    public CheckedInMethod Method { get; set; }
}
