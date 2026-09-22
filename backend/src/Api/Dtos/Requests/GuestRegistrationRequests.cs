using System.ComponentModel.DataAnnotations;
using Application.GuestManagement;

namespace Api.Dtos.Requests;

public class RegistrationFormRequest
{
    public DateTimeOffset OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    [Range(1, int.MaxValue)] public int SeatLimit { get; set; }
    public FormSettings ToSettings() => new(OpensAt, ClosesAt, SeatLimit);
}

public class SeatLimitRequest
{
    [Range(1, int.MaxValue)] public int SeatLimit { get; set; }
}

public class SubmitRegistrationRequest
{
    [Required, StringLength(200)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string EmailAddress { get; set; } = string.Empty;
    [StringLength(200)] public string? Organisation { get; set; }
    [StringLength(40)] public string? PhoneNumber { get; set; }
    [MaxLength(10)] public List<AnswerSubmission>? Answers { get; set; }
    public GuestDetails ToDetails() => new(FullName, EmailAddress, Organisation, PhoneNumber);
}

public class SelectRegistrationQuestionsRequest
{
    [Required, MaxLength(10)] public List<QuestionSelection> Questions { get; set; } = [];
}

public class RegistrationAccessRequest
{
    [Required, StringLength(43, MinimumLength = 43)] public string Secret { get; set; } = string.Empty;
}

public class RegistrationRsvpRequest : RegistrationAccessRequest
{
    [Required, StringLength(20)] public string Response { get; set; } = string.Empty;
}

public class ValidateInvitationRequest
{
    [Required, StringLength(43, MinimumLength = 43)] public string Token { get; set; } = string.Empty;
}
