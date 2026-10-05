using Domain.Entities;
using Domain.Enums;

namespace Application.GuestManagement;

public record FormSettings(DateTimeOffset OpensAt, DateTimeOffset ClosesAt, int SeatLimit);
public record GuestDetails(string FullName, string EmailAddress, string? Organisation, string? PhoneNumber);
public record RegistrationReceipt(RegistrationSubmission Registration, string StatusSecret);
public record RegistrationPage(IReadOnlyList<RegistrationSubmission> Items, int Total, int Page, int PageSize);
public record InvitationEmail(string EmailAddress, string FullName, string EventName,
    DateTimeOffset StartsAt, string? Location, string Token, byte[] QrPng, string? StatusUrl = null);
public record RejectionEmail(string EmailAddress, string FullName, string EventName);
public record RegistrationLinkEmail(string EmailAddress, string FullName, string EventName, string RegistrationUrl);

public enum EmailDeliveryResult { SENT, UNAVAILABLE, FAILED }

public interface IInvitationEmailSender
{
    Task<EmailDeliveryResult> SendAsync(InvitationEmail invitation, CancellationToken cancellationToken);
}

public interface IRegistrationOutcomeEmailSender
{
    Task<EmailDeliveryResult> SendRejectionAsync(RejectionEmail rejection, CancellationToken cancellationToken);
}

public interface IRegistrationLinkEmailSender
{
    Task<EmailDeliveryResult> SendRegistrationLinkAsync(RegistrationLinkEmail invitation, CancellationToken cancellationToken);
}

public interface IRegistrationTokenGenerator
{
    string Generate();
    string Hash(string secret);
    bool Matches(string secret, string hash);
    byte[] CreateQrPng(string token);
}

public interface IRegistrationSecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}

public sealed class GuestRegistrationOptions
{
    public string? PublicWebBaseUrl { get; set; }
    public bool AllowInsecureLocalhost { get; set; }

    public Uri GetPublicWebBaseUri()
    {
        if (!Uri.TryCreate(PublicWebBaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            (uri.Scheme == "http" && (!AllowInsecureLocalhost || !uri.IsLoopback)))
            throw new RegistrationException(503, "public_web_url_unavailable",
                "A valid public web base URL is required; HTTPS is required outside Development.");
        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
    }
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
