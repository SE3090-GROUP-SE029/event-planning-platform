using System.Net;
using System.Net.Mail;
using System.Text;
using Application.GuestManagement;
using Microsoft.Extensions.Logging;

namespace Infrastructure.ExternalServices;

public class EmailSender(SmtpEmailOptions options, ILogger<EmailSender> logger) : IInvitationEmailSender
{
    public Task<EmailDeliveryResult> SendAsync(InvitationEmail invitation, CancellationToken cancellationToken)
        => SendMessageAsync(invitation.EmailAddress, "Your event invitation",
            $"Hello {invitation.FullName},\n\nYour place at {invitation.EventName} is confirmed.\n" +
            $"Starts: {invitation.StartsAt:O}\nLocation: {invitation.Location ?? "Not specified"}\n\n" +
            $"Invitation token: {invitation.Token}\nYour QR code is attached. Keep this invitation private.",
            invitation.QrPng, cancellationToken);

    public Task<EmailDeliveryResult> SendRejectionAsync(RejectionEmail rejection, CancellationToken cancellationToken)
        => SendMessageAsync(rejection.EmailAddress, "Your event registration",
            $"Hello {rejection.FullName},\n\nThank you for your interest in {rejection.EventName}. " +
            "We are unable to accept your registration for this event.\n\nThank you for your understanding.", null, cancellationToken);

    private async Task<EmailDeliveryResult> SendMessageAsync(string recipient, string subject, string body,
        byte[]? qrPng, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Host) || !MailAddress.TryCreate(options.FromAddress, out var from) ||
            options.Port is < 1 or > 65535 || options.TimeoutSeconds is < 1 or > 60)
        {
            logger.LogWarning("Invitation delivery is pending because SMTP configuration is unavailable or invalid.");
            return EmailDeliveryResult.UNAVAILABLE;
        }
        // Never transmit SMTP credentials without transport encryption.
        if (!options.EnableSsl && !string.IsNullOrWhiteSpace(options.UserName))
        {
            logger.LogWarning("Invitation delivery is pending because authenticated SMTP requires TLS.");
            return EmailDeliveryResult.UNAVAILABLE;
        }
        using var message = new MailMessage
        {
            From = from, Subject = subject, BodyEncoding = Encoding.UTF8, Body = body
        };
        message.To.Add(new MailAddress(recipient));
        if (qrPng is not null) message.Attachments.Add(new Attachment(new MemoryStream(qrPng), "invitation-qr.png", "image/png"));
        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.EnableSsl, UseDefaultCredentials = false };
        if (!string.IsNullOrWhiteSpace(options.UserName)) client.Credentials = new NetworkCredential(options.UserName, options.Password);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
        try
        {
            await client.SendMailAsync(message, timeout.Token);
            return EmailDeliveryResult.SENT;
        }
        catch (Exception exception) when (exception is SmtpException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning("Registration email delivery failed; delivery can be retried.");
            return EmailDeliveryResult.FAILED;
        }
    }
}
