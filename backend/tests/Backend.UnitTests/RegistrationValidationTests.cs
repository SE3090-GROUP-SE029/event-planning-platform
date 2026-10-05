using Application.GuestManagement;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Backend.UnitTests;

public class RegistrationValidationTests
{
    [Theory]
    [InlineData("", "guest@example.com")]
    [InlineData("   ", "guest@example.com")]
    [InlineData("Guest", "")]
    [InlineData("Guest", "invalid")]
    [InlineData("Guest", "a@@example.com")]
    [InlineData("Guest", "Display <guest@example.com>")]
    [InlineData("Guest\nInjected", "guest@example.com")]
    public void RejectsInvalidRequiredFields(string name, string email)
        => Assert.Equal(400, Assert.Throws<RegistrationException>(() => RegistrationValidator.Validate(new GuestDetails(name, email, null, null))).StatusCode);

    [Fact]
    public void TrimsFieldsAndPreservesOptionalValues()
    {
        var result = RegistrationValidator.Validate(new GuestDetails(" Guest Name ", " guest@example.com ", " Organisation ", " +94 123456789 "));
        Assert.Equal(new GuestDetails("Guest Name", "guest@example.com", "Organisation", "+94 123456789"), result);
        Assert.Null(RegistrationValidator.Validate(result with { Organisation = " " }).Organisation);
    }

    [Theory]
    [InlineData(201, 10, 0, 0)]
    [InlineData(10, 255, 0, 0)]
    [InlineData(10, 10, 201, 0)]
    [InlineData(10, 10, 0, 41)]
    public void RejectsOversizedFields(int nameLength, int emailLength, int organisationLength, int phoneLength)
    {
        var details = new GuestDetails(new string('a', nameLength), new string('a', emailLength) + "@example.com",
            new string('a', organisationLength), new string('1', phoneLength));
        Assert.Throws<RegistrationException>(() => RegistrationValidator.Validate(details));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectsInvalidCapacity(int capacity)
        => Assert.Throws<RegistrationException>(() => RegistrationValidator.ValidateSeatLimit(capacity));

    [Fact]
    public void ValidatesPeriodAndNormalizesUtc()
    {
        var opens = new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.FromHours(5.5));
        var result = RegistrationValidator.Validate(new FormSettings(opens, opens.AddDays(1), 2));
        Assert.Equal(TimeSpan.Zero, result.OpensAt.Offset);
        Assert.Equal(opens, result.OpensAt);
        Assert.Throws<RegistrationException>(() => RegistrationValidator.Validate(new FormSettings(opens, opens, 2)));
        Assert.Throws<RegistrationException>(() => RegistrationValidator.Validate(new FormSettings(opens, opens.AddDays(-1), 2)));
    }

    [Fact]
    public void GeneratesIndependentUniqueOpaqueCredentialsAndVerifiesHash()
    {
        var generator = new RegistrationTokenGenerator();
        var credentials = Enumerable.Range(0, 1000).Select(_ => generator.Generate()).ToArray();
        Assert.Equal(1000, credentials.Distinct().Count());
        Assert.All(credentials, RegistrationValidator.ValidatePublicCredential);
        var hash = generator.Hash(credentials[0]);
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain(credentials[0], hash);
        Assert.True(generator.Matches(credentials[0], hash));
        Assert.False(generator.Matches(credentials[1], hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("internal-id-1")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/")]
    public void RejectsMalformedPublicCredentials(string value)
        => Assert.Throws<RegistrationException>(() => RegistrationValidator.ValidatePublicCredential(value));

    [Fact]
    public void RendersTokenAsRealPng()
    {
        var generator = new RegistrationTokenGenerator();
        var png = generator.CreateQrPng(generator.Generate());
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.True(png.Length > 100);
    }

    [Fact]
    public void ProtectsAndRestoresStatusSecretsWithoutStoringPlaintext()
    {
        var protector = new RegistrationSecretProtector(DataProtectionProvider.Create("RegistrationTests"));
        const string secret = "test-status-secret";

        var protectedSecret = protector.Protect(secret);

        Assert.NotEqual(secret, protectedSecret);
        Assert.Equal(secret, protector.Unprotect(protectedSecret));
    }

    [Theory]
    [InlineData("https://events.example.com", "https://events.example.com/")]
    public void AcceptsHostedHttpsPublicUrls(string configuredUrl, string expectedUrl)
        => Assert.Equal(expectedUrl, new GuestRegistrationOptions
        {
            PublicWebBaseUrl = configuredUrl
        }.GetPublicWebBaseUri().AbsoluteUri);

    [Theory]
    [InlineData("http://localhost:5173", "http://localhost:5173/")]
    [InlineData("http://127.0.0.1:5173/", "http://127.0.0.1:5173/")]
    public void AcceptsLoopbackHttpOnlyWhenDevelopmentAllowsIt(
        string configuredUrl,
        string expectedUrl)
        => Assert.Equal(expectedUrl, new GuestRegistrationOptions
        {
            PublicWebBaseUrl = configuredUrl,
            AllowInsecureLocalhost = true
        }.GetPublicWebBaseUri().AbsoluteUri);

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://events.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:password@events.example.com")]
    public void RejectsUnsafePublicWebUrls(string configuredUrl)
        => Assert.Equal("public_web_url_unavailable",
            Assert.Throws<RegistrationException>(() => new GuestRegistrationOptions
            {
                PublicWebBaseUrl = configuredUrl
            }.GetPublicWebBaseUri()).Code);

    [Fact]
    public async Task MissingSmtpIsUnavailableInsteadOfFalseSuccess()
    {
        var sender = new EmailSender(new SmtpEmailOptions(), NullLogger<EmailSender>.Instance);
        var result = await sender.SendAsync(new InvitationEmail("guest@example.com", "Guest", "Event",
            DateTimeOffset.UtcNow, null, "token", []), CancellationToken.None);
        Assert.Equal(EmailDeliveryResult.UNAVAILABLE, result);
    }

    [Fact]
    public async Task AuthenticatedSmtpRequiresTls()
    {
        var sender = new EmailSender(new SmtpEmailOptions { Host = "localhost", FromAddress = "events@example.com", UserName = "sender", EnableSsl = false },
            NullLogger<EmailSender>.Instance);
        var result = await sender.SendAsync(new InvitationEmail("guest@example.com", "Guest", "Event",
            DateTimeOffset.UtcNow, null, "token", []), CancellationToken.None);
        Assert.Equal(EmailDeliveryResult.UNAVAILABLE, result);
    }

    [Fact]
    public void ExpiredRevokedAndWaitingInvitationsAreNotValid()
    {
        var service = new InvitationService(null!, null!, null!, null!, null!, TimeProvider.System);
        var registration = new RegistrationSubmission
        {
            Status = RegistrationStatus.CONFIRMED,
            Invitation = new Invitation { TokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1) }
        };
        Assert.True(service.IsActive(registration));
        registration.Invitation.RevokedAt = DateTimeOffset.UtcNow;
        Assert.False(service.IsActive(registration));
        registration.Invitation.RevokedAt = null;
        registration.Status = RegistrationStatus.WAITLISTED;
        Assert.False(service.IsActive(registration));
        registration.Status = RegistrationStatus.CONFIRMED;
        registration.Invitation.TokenExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);
        Assert.False(service.IsActive(registration));
    }
}
