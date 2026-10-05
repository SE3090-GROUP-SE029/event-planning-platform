using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Application.GuestManagement;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.IntegrationTests;

public class SmtpDeliveryTests
{
    [Fact]
    public async Task InvitationWithCredentialsRequiresTlsWithoutSmtpAuthentication()
    {
        var tokens = new RegistrationTokenGenerator();
        var token = tokens.Generate();
        var qrPng = tokens.CreateQrPng(token);
        const string statusUrl = "https://events.example.com/guest/status/reference#secret=status-secret";
        var sender = new EmailSender(new SmtpEmailOptions
        {
            Host = "127.0.0.1", Port = 1, FromAddress = "events@example.com", EnableSsl = false, TimeoutSeconds = 5
        }, NullLogger<EmailSender>.Instance);
        var result = await sender.SendAsync(new InvitationEmail("guest@example.com", "Guest", "Example Event",
            DateTimeOffset.UtcNow, "Colombo", token, qrPng, statusUrl), CancellationToken.None);
        Assert.Equal(EmailDeliveryResult.UNAVAILABLE, result);
    }

    [Fact]
    public async Task RegistrationLinkEmailContainsHostedPublicFormUrl()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var receive = ReceiveAsync(listener, false);
        var sender = new EmailSender(new SmtpEmailOptions
        {
            Host = "127.0.0.1",
            Port = ((IPEndPoint)listener.LocalEndpoint).Port,
            FromAddress = "events@example.com",
            EnableSsl = false,
            TimeoutSeconds = 5
        }, NullLogger<EmailSender>.Instance);
        const string registrationUrl = "https://events.example.com/guest/register/public-id";

        var result = await sender.SendRegistrationLinkAsync(
            new RegistrationLinkEmail("guest@example.com", "Guest", "Example Event", registrationUrl),
            CancellationToken.None);
        var message = await receive.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(EmailDeliveryResult.SENT, result);
        var body = Regex.Match(message, @"Content-Type: text/plain; charset=utf-8\s+Content-Transfer-Encoding: base64\s+(?<data>[A-Za-z0-9+/=\r\n]+)");
        Assert.True(body.Success);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(body.Groups["data"].Value));
        Assert.Contains(registrationUrl, decoded);
        Assert.Contains("not complete until you submit", decoded);
    }

    private static async Task<string> ReceiveAsync(TcpListener listener, bool rejectRecipient)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        await using var writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteLineAsync("220 localhost test SMTP");
        var message = new StringBuilder();
        while (await reader.ReadLineAsync() is { } line)
        {
            if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 End with a dot");
                while (await reader.ReadLineAsync() is { } body && body != ".") message.AppendLine(body);
                await writer.WriteLineAsync("250 Accepted");
            }
            else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("221 Bye");
                break;
            }
            else if (rejectRecipient && line.StartsWith("RCPT", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("550 Recipient rejected for test");
            }
            else await writer.WriteLineAsync("250 OK");
        }
        return message.ToString();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectionUsesExistingSmtpTransportWithoutQrOrInternalAnalysis(bool rejectRecipient)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var receive = ReceiveAsync(listener, rejectRecipient);
        var sender = new EmailSender(new SmtpEmailOptions { Host = "127.0.0.1", Port = ((IPEndPoint)listener.LocalEndpoint).Port,
            FromAddress = "events@example.com", EnableSsl = false, TimeoutSeconds = 5 }, NullLogger<EmailSender>.Instance);
        Assert.Equal(rejectRecipient ? EmailDeliveryResult.FAILED : EmailDeliveryResult.SENT,
            await sender.SendRejectionAsync(new("guest@example.com", "Guest", "Example Event"), CancellationToken.None));
        var message = await receive.WaitAsync(TimeSpan.FromSeconds(10));
        if (rejectRecipient) return;
        var body = Regex.Match(message, @"Content-Type: text/plain; charset=utf-8\s+Content-Transfer-Encoding: base64\s+(?<data>[A-Za-z0-9+/=\r\n]+)");
        Assert.True(body.Success);
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(body.Groups["data"].Value));
        Assert.Contains("unable to accept your registration", decoded);
        Assert.Contains("Example Event", decoded);
        foreach (var internalField in new[] { "confidence", "qwen", "prompt", "flags", "token" }) Assert.DoesNotContain(internalField, decoded);
        Assert.DoesNotContain("image/png", message);
        Assert.DoesNotContain("invitation-qr.png", message);
    }
}
