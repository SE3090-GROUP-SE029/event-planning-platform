using Domain.Entities;

namespace Api.Dtos.Responses;

public record GuestAiReviewResponse(string Status, string? Decision, double? Confidence,
    string[] Reasons, string[] Flags, string? Model, string? PromptVersion, DateTimeOffset RequestedAt,
    DateTimeOffset? AnalyzedAt, int Attempts, string? FailureCode)
{
    public static GuestAiReviewResponse From(GuestAiReview review) => new(review.Status.ToString(),
        review.Decision?.ToString(), review.Confidence, review.Reasons, review.Flags, review.Model,
        review.PromptVersion, review.RequestedAt, review.AnalyzedAt, review.Attempts, review.FailureCode);
}

public record PlannerFormResponse(Guid Id, Guid EventId, DateTimeOffset OpensAt, DateTimeOffset ClosesAt,
    int SeatLimit, string Status, string? PublicId, string? PublicPath, DateTimeOffset? PublishedAt, FormQuestionResponse[] Questions)
{
    public static PlannerFormResponse From(RegistrationForm form) => new(form.Id, form.EventId, form.OpensAt,
        form.ClosesAt, form.SeatLimit, form.Status.ToString(), form.PublicId,
        form.PublicId is null ? null : $"/api/public/registration-forms/{form.PublicId}", form.PublishedAt, FormQuestionResponse.From(form));
}

public record PublicFormResponse(string PublicId, string EventName, string? Description,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Location, DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt, int SeatLimit, bool IsOpen, string[] RequiredFields, string[] OptionalFields, FormQuestionResponse[] Questions);

public record FormQuestionResponse(Guid Id, string Question, bool Required)
{
    public static FormQuestionResponse[] From(RegistrationForm form) => form.Questions.Where(q => q.IsSelected)
        .OrderBy(q => q.DisplayOrder).Select(q => new FormQuestionResponse(q.Id, q.Question, q.Required)).ToArray();
}
public record RegistrationAnswerResponse(Guid QuestionId, string Answer);

public record PublicRegistrationResponse(string PublicReference, string Status, string? StatusSecret,
    string? InvitationToken, string? QrPngBase64, string? RsvpStatus, string? EmailDeliveryStatus,
    DateTimeOffset RegisteredAt, DateTimeOffset? ConfirmedAt, DateTimeOffset? CancelledAt);

public record PlannerRegistrationResponse(long Id, string FullName, string EmailAddress, string? Organisation,
    string? PhoneNumber, string Status, string? RsvpStatus, string? EmailDeliveryStatus,
    DateTimeOffset RegisteredAt, DateTimeOffset? ConfirmedAt, DateTimeOffset? CancelledAt,
    int DeliveryAttempts, DateTimeOffset? SentAt, RegistrationAnswerResponse[] Answers)
{
    public static PlannerRegistrationResponse From(RegistrationSubmission registration) => new(registration.Id,
        registration.Guest.FullName, registration.Guest.EmailAddress, registration.Guest.Organisation,
        registration.Guest.PhoneNumber, registration.Status.ToString(), registration.Invitation?.RsvpStatus.ToString(),
        registration.RejectionDeliveryStatus?.ToString() ?? registration.Invitation?.DeliveryStatus.ToString(), registration.RegisteredAt, registration.ConfirmedAt,
        registration.CancelledAt, registration.RejectionDeliveryStatus is not null ? registration.RejectionDeliveryAttempts : registration.Invitation?.DeliveryAttempts ?? 0,
        registration.RejectionSentAt ?? registration.Invitation?.SentAt,
        registration.Answers.Select(a => new RegistrationAnswerResponse(a.RegistrationQuestionId, a.Answer)).ToArray());
}
