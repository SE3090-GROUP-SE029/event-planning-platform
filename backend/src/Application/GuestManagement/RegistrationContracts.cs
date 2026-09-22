using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public record FormSettings(DateTimeOffset OpensAt, DateTimeOffset ClosesAt, int SeatLimit);
public record GuestDetails(string FullName, string EmailAddress, string? Organisation, string? PhoneNumber);
public record RegistrationReceipt(RegistrationSubmission Registration, string StatusSecret);
public record RegistrationPage(IReadOnlyList<RegistrationSubmission> Items, int Total, int Page, int PageSize);
public record InvitationEmail(string EmailAddress, string FullName, string EventName,
    DateTimeOffset StartsAt, string? Location, string Token, byte[] QrPng);
public record RejectionEmail(string EmailAddress, string FullName, string EventName);

public enum EmailDeliveryResult { SENT, UNAVAILABLE, FAILED }

public interface IInvitationEmailSender
{
    Task<EmailDeliveryResult> SendAsync(InvitationEmail invitation, CancellationToken cancellationToken);
    Task<EmailDeliveryResult> SendRejectionAsync(RejectionEmail rejection, CancellationToken cancellationToken);
}

public interface IRegistrationTokenGenerator
{
    string Generate();
    string Hash(string secret);
    bool Matches(string secret, string hash);
    byte[] CreateQrPng(string token);
}

// Additional deterministic eligibility checks remain independent of AI and capacity allocation.
public interface IRegistrationEligibilityPolicy
{
    Task<bool> IsEligibleAsync(Guest guest, Event eventDetails, CancellationToken cancellationToken);
}

public class ValidatedRegistrationEligibilityPolicy : IRegistrationEligibilityPolicy
{
    public Task<bool> IsEligibleAsync(Guest guest, Event eventDetails, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

public class RegistrationException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}
