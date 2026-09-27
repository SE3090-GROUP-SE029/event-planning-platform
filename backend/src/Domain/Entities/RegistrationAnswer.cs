namespace Domain.Entities;

public class RegistrationAnswer
{
    public long RegistrationSubmissionId { get; set; }
    public RegistrationSubmission RegistrationSubmission { get; set; } = null!;
    public Guid RegistrationQuestionId { get; set; }
    public RegistrationQuestion RegistrationQuestion { get; set; } = null!;
    public Guid RegistrationFormId { get; set; }
    public string Answer { get; set; } = string.Empty;
}
