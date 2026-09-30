using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

/// <summary>
/// Integration tests for the planner guest list CSV upload endpoint.
/// POST /api/events/{eventId}/registration-form/upload
/// </summary>
public class GuestListUploadEndpointTests(RegistrationHostFixture fixture) : IClassFixture<RegistrationHostFixture>
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static string UploadPath(Guid eventId) => $"/api/events/{eventId}/registration-form/upload";

    private async Task<HttpResponseMessage> UploadCsvAsync(Guid eventId, string csvContent,
        string? plannerId = null, string fileName = "guests.csv")
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(eventId))
        { Content = content };
        if (plannerId is not null)
            request.Headers.Add("X-Test-Identity", plannerId);
        return await fixture.Client.SendAsync(request);
    }

    private async Task<JsonElement> JsonAsync(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}; got {response.StatusCode}: {text}");
        using var json = JsonDocument.Parse(text);
        return json.RootElement.Clone();
    }

    [Fact]
    public async Task UnauthenticatedRequestReturns401()
    {
        var eventDetails = await fixture.CreateEventAsync();
        var response = await UploadCsvAsync(eventDetails.Id, "fullName,emailAddress\nAlice,alice@example.com");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongEventOwnerReturns404()
    {
        var eventDetails = await fixture.CreateEventAsync("owner-a");
        var wrongPlanner = RegistrationHostFixture.GuestOwnerGuid("owner-b").ToString();
        // Publish form first — use the right planner for that
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.GuestOwnerGuid("owner-a").ToString(),
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.GuestOwnerGuid("owner-a").ToString(), Ct));

        var response = await UploadCsvAsync(eventDetails.Id, "fullName,emailAddress\nAlice,alice@example.com", wrongPlanner);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NoFileReturns400()
    {
        var eventDetails = await fixture.CreateEventAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(eventDetails.Id))
        { Content = new MultipartFormDataContent() };
        request.Headers.Add("X-Test-Identity", RegistrationHostFixture.PlannerId);
        var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingFormReturns409()
    {
        var eventDetails = await fixture.CreateEventAsync();
        var response = await UploadCsvAsync(eventDetails.Id,
            "fullName,emailAddress\nAlice,alice@example.com",
            RegistrationHostFixture.PlannerId);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnpublishedFormReturns409()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        var response = await UploadCsvAsync(eventDetails.Id,
            "fullName,emailAddress\nAlice,alice@example.com",
            RegistrationHostFixture.PlannerId);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ValidUploadCreatesGuestAndRegistrationWithPendingAi()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 100), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var csv = "fullName,emailAddress,organisation,phoneNumber\n" +
                  "Alice Smith,alice@upload.com,SLIIT,+94111111111\n" +
                  "Bob Jones,bob@upload.com,,\n";

        var response = await UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId);
        var result = await JsonAsync(response);

        Assert.Equal(2, result.GetProperty("totalRows").GetInt32());
        Assert.Equal(2, result.GetProperty("successfulRows").GetInt32());
        Assert.Equal(0, result.GetProperty("failedRows").GetInt32());
        Assert.Equal(0, result.GetProperty("duplicateRows").GetInt32());
        Assert.Equal(0, result.GetProperty("alreadyRegisteredRows").GetInt32());

        await using var db = fixture.CreateDb();
        var subs = await db.RegistrationSubmissions
            .Include(s => s.Guest)
            .Where(s => s.EventId == eventDetails.Id)
            .ToListAsync();
        Assert.Equal(2, subs.Count);
        Assert.All(subs, s => Assert.Equal(RegistrationStatus.PENDING_AI, s.Status));
        var aliceGuest = subs.Single(s => s.Guest.EmailAddress == "alice@upload.com");
        Assert.Equal("SLIIT", aliceGuest.Guest.Organisation);
        Assert.Equal("+94111111111", aliceGuest.Guest.PhoneNumber);

        // Verify AI review records were created by AddRegistration.
        var reviews = await db.GuestAiReviews
            .Where(r => subs.Select(s => s.Id).Contains(r.RegistrationSubmissionId))
            .ToListAsync();
        Assert.Equal(2, reviews.Count);
    }

    [Fact]
    public async Task IntraFileDuplicateEmailsAreSkipped()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 100), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var csv = "fullName,emailAddress\n" +
                  "Alice,dup@upload.com\n" +
                  "Alice2,DUP@UPLOAD.COM\n"; // same email, different case

        var response = await UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId);
        var result = await JsonAsync(response);

        Assert.Equal(1, result.GetProperty("successfulRows").GetInt32());
        Assert.Equal(1, result.GetProperty("duplicateRows").GetInt32());
        Assert.Equal(1, result.GetProperty("errors").GetArrayLength());

        await using var db = fixture.CreateDb();
        Assert.Equal(1, await db.Guests.CountAsync(g => g.EventId == eventDetails.Id));
    }

    [Fact]
    public async Task AlreadyRegisteredEmailsAreSkippedNotFailures()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 100), Ct));
        var form = await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        // Register Alice via the public flow first.
        await fixture.WithServiceAsync(s => s.SubmitAsync(
            form.PublicId!, new Application.GuestManagement.GuestDetails("Alice", "alice@existing.com", null, null), [], Ct));

        // Upload the same email again.
        var csv = "fullName,emailAddress\nAlice,alice@existing.com\n";
        var response = await UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId);
        var result = await JsonAsync(response);

        Assert.Equal(0, result.GetProperty("successfulRows").GetInt32());
        Assert.Equal(1, result.GetProperty("alreadyRegisteredRows").GetInt32());
        Assert.Equal(0, result.GetProperty("failedRows").GetInt32());
    }

    [Fact]
    public async Task InvalidCsvRowsAreSurfacedWithoutHaltingOthers()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 100), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var csv = "fullName,emailAddress\n" +
                  ",bad-row@upload.com\n" +           // missing name
                  "Valid,valid@upload.com\n" +         // valid
                  "Another,not-an-email\n";            // invalid email

        var response = await UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId);
        var result = await JsonAsync(response);

        Assert.Equal(1, result.GetProperty("successfulRows").GetInt32());
        Assert.Equal(2, result.GetProperty("failedRows").GetInt32());
        Assert.Equal(2, result.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task UploadedGuestsEnterAiPipelineWhenWorkerRuns()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var csv = "fullName,emailAddress\nPipeline,pipeline@upload.com\n";
        await UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId);

        // Drive the existing AI worker — same as the public registration flow.
        await fixture.DrainAiAsync();

        await using var db = fixture.CreateDb();
        var sub = await db.RegistrationSubmissions
            .SingleAsync(s => s.EventId == eventDetails.Id);

        // With 10 seats and 1 submission the guest should be CONFIRMED.
        Assert.Equal(RegistrationStatus.CONFIRMED, sub.Status);
        Assert.True(fixture.Email.Messages.Any());
    }
}
