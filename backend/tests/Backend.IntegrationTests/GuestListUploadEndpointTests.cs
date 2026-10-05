using System.Net;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

/// <summary>
/// Integration tests for the planner guest list CSV upload endpoint.
/// POST /api/events/{eventId}/registration-form/upload
/// </summary>
public class GuestListUploadEndpointTests : IClassFixture<RegistrationHostFixture>
{
    private readonly RegistrationHostFixture fixture;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public GuestListUploadEndpointTests(RegistrationHostFixture fixture)
    {
        this.fixture = fixture;
        fixture.Email.Messages.Clear();
        fixture.Email.Rejections.Clear();
        fixture.Email.RegistrationLinks.Clear();
    }

    private static string UploadPath(Guid eventId) => $"/api/events/{eventId}/registration-form/upload";

    private async Task<HttpResponseMessage> UploadCsvAsync(Guid eventId, string csvContent,
        string? plannerId = null, string fileName = "guests.csv", bool drainEmailQueue = true)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(eventId))
        { Content = content };
        if (plannerId is not null)
            request.Headers.Add("X-Test-Identity", plannerId);
        var response = await fixture.Client.SendAsync(request);
        if (drainEmailQueue) await DrainEmailQueueAsync();
        return response;
    }

    private async Task<HttpResponseMessage> UploadDocxAsync(
        Guid eventId,
        byte[] bytes,
        string? plannerId = null,
        bool drainEmailQueue = true)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Add(fileContent, "file", "guests.docx");

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(eventId))
        { Content = content };
        if (plannerId is not null)
            request.Headers.Add("X-Test-Identity", plannerId);
        var response = await fixture.Client.SendAsync(request);
        if (drainEmailQueue) await DrainEmailQueueAsync();
        return response;
    }

    private async Task<HttpResponseMessage> UploadPdfAsync(Guid eventId, byte[] bytes, string? plannerId = null)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "guests.pdf");

        using var request = new HttpRequestMessage(HttpMethod.Post, UploadPath(eventId))
        { Content = content };
        if (plannerId is not null)
            request.Headers.Add("X-Test-Identity", plannerId);
        var response = await fixture.Client.SendAsync(request);
        await DrainEmailQueueAsync();
        return response;
    }

    private async Task DrainEmailQueueAsync()
    {
        for (var index = 0; index < 1000; index++)
        {
            if (!await fixture.WithEmailDeliveryServiceAsync(service => service.ProcessNextAsync(Ct)))
                return;
        }

        throw new InvalidOperationException("Unexpected registration-link email queue backlog.");
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
    public async Task CreatingAndPublishingFormEnablesGuestUpload()
    {
        var eventDetails = await fixture.CreateEventAsync();
        using var createRequest = new HttpRequestMessage(
            HttpMethod.Post, $"/api/events/{eventDetails.Id}/registration-form")
        {
            Content = JsonContent.Create(new
            {
                opensAt = RegistrationHostFixture.Now.AddHours(-1),
                closesAt = RegistrationHostFixture.Now.AddDays(5),
                seatLimit = 10,
            }),
        };
        createRequest.Headers.Add("X-Test-Identity", RegistrationHostFixture.PlannerId);
        var createResponse = await fixture.Client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var publishRequest = new HttpRequestMessage(
            HttpMethod.Post, $"/api/events/{eventDetails.Id}/registration-form/publish");
        publishRequest.Headers.Add("X-Test-Identity", RegistrationHostFixture.PlannerId);
        var publishResponse = await fixture.Client.SendAsync(publishRequest);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var uploadResponse = await UploadCsvAsync(
            eventDetails.Id,
            "fullName,emailAddress\nAlice,alice@lifecycle.com",
            RegistrationHostFixture.PlannerId);
        var upload = await JsonAsync(uploadResponse);
        Assert.Equal(1, upload.GetProperty("successfulRows").GetInt32());
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
    public async Task ValidUploadEmailsRegistrationLinksWithoutCreatingRegistrations()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 100), Ct));
        var published = await fixture.WithServiceAsync(s => s.PublishAsync(
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
        var uploadedGuests = await db.Guests.Where(g => g.EventId == eventDetails.Id)
            .OrderBy(g => g.EmailAddress).ToListAsync();
        Assert.Equal(2, uploadedGuests.Count);
        Assert.Equal("Alice Smith", uploadedGuests[0].FullName);
        Assert.Equal("SLIIT", uploadedGuests[0].Organisation);
        Assert.Equal("+94111111111", uploadedGuests[0].PhoneNumber);
        Assert.Equal("bob@upload.com", uploadedGuests[1].EmailAddress);
        Assert.False(await db.RegistrationSubmissions.AnyAsync(s => s.EventId == eventDetails.Id));
        Assert.False(await db.Invitations.AnyAsync(i => i.RegistrationSubmission.EventId == eventDetails.Id));
        Assert.False(await db.GuestAiReviews.AnyAsync(r => r.RegistrationSubmission.EventId == eventDetails.Id));
        var links = fixture.Email.RegistrationLinks.Where(i => i.EmailAddress.EndsWith("@upload.com")).ToArray();
        Assert.Equal(2, links.Length);
        Assert.All(links, link => Assert.EndsWith($"/guest/register/{published.PublicId}", link.RegistrationUrl));
        Assert.All(links, link => Assert.Equal("Guest Management Integration Event", link.EventName));
    }

    [Fact]
    public async Task ValidDocxTablesParagraphsAndNestedTablesCreateGuestsAndSendLinks()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 100), Ct));
        var published = await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));
        var bytes = CreateDocx(
            new Paragraph(new Run(new Text("Paragraph Guest paragraph@docx-upload.test"))),
            new Table(
                Row("Full Name", "Email Address"),
                Row("Table Guest", "table@docx-upload.test"),
                new TableRow(new TableCell(new Table(
                    Row("Nested Guest", "nested@docx-upload.test"))))));

        fixture.Email.RegistrationLinkDelay = TimeSpan.FromSeconds(2);
        var uploadStarted = Stopwatch.GetTimestamp();
        var response = await UploadDocxAsync(
            eventDetails.Id, bytes, RegistrationHostFixture.PlannerId, drainEmailQueue: false);
        var uploadElapsed = Stopwatch.GetElapsedTime(uploadStarted);
        fixture.Email.RegistrationLinkDelay = TimeSpan.Zero;
        var result = await JsonAsync(response);

        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal(3, result.GetProperty("uploadedGuests").GetInt32());
        Assert.Equal(3, result.GetProperty("queuedEmails").GetInt32());
        Assert.Equal(0, result.GetProperty("invalidRows").GetInt32());
        Assert.Equal(3, result.GetProperty("successfulRows").GetInt32());
        Assert.True(uploadElapsed < TimeSpan.FromSeconds(1.5),
            $"Upload waited for SMTP delivery: {uploadElapsed.TotalMilliseconds:F0} ms.");
        await using var db = fixture.CreateDb();
        Assert.Equal(3, await db.Guests.CountAsync(g => g.EventId == eventDetails.Id));
        Assert.Equal(3, await db.RegistrationLinkEmailJobs.CountAsync(job =>
            job.EventId == eventDetails.Id && job.Status == RegistrationLinkEmailDeliveryStatus.QUEUED));
        Assert.Empty(fixture.Email.RegistrationLinks);

        await DrainEmailQueueAsync();
        var links = fixture.Email.RegistrationLinks;
        Assert.Equal(3, links.Count);
        Assert.All(links, link => Assert.EndsWith($"/guest/register/{published.PublicId}", link.RegistrationUrl));
        Assert.Equal(3, await db.RegistrationLinkEmailJobs.CountAsync(job =>
            job.EventId == eventDetails.Id && job.Status == RegistrationLinkEmailDeliveryStatus.SENT));
    }

    [Fact]
    public async Task LargeDocxPersistsAndQueuesOneThousandGuestsWithoutWaitingForMail()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 2000), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));
        var rows = new List<OpenXmlElement> { Row("Full Name", "Email Address") };
        rows.AddRange(Enumerable.Range(0, 1000).Select(index =>
            (OpenXmlElement)Row($"Large Guest {index}", $"large{index}@docx-upload.test")));
        var bytes = CreateDocx(new Table(rows));

        var response = await UploadDocxAsync(
            eventDetails.Id, bytes, RegistrationHostFixture.PlannerId, drainEmailQueue: false);
        var result = await JsonAsync(response);

        Assert.Equal(1000, result.GetProperty("uploadedGuests").GetInt32());
        Assert.Equal(1000, result.GetProperty("queuedEmails").GetInt32());
        await using var db = fixture.CreateDb();
        Assert.Equal(1000, await db.Guests.CountAsync(g => g.EventId == eventDetails.Id));
        Assert.Equal(1000, await db.RegistrationLinkEmailJobs.CountAsync(job => job.EventId == eventDetails.Id));
        Assert.Empty(fixture.Email.RegistrationLinks);
        var queuedJobs = await db.RegistrationLinkEmailJobs
            .Where(job => job.EventId == eventDetails.Id)
            .ToListAsync();
        db.RegistrationLinkEmailJobs.RemoveRange(queuedJobs);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ValidPdfCreatesGuestAndQueuesRegistrationLink()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var response = await UploadPdfAsync(
            eventDetails.Id,
            CreatePdf("PDF Guest pdf-upload@example.test"),
            RegistrationHostFixture.PlannerId);
        var result = await JsonAsync(response);

        Assert.Equal(1, result.GetProperty("uploadedGuests").GetInt32());
        Assert.Equal(1, result.GetProperty("queuedEmails").GetInt32());
        await using var db = fixture.CreateDb();
        var guest = await db.Guests.SingleAsync(g => g.EventId == eventDetails.Id);
        Assert.Equal("PDF Guest", guest.FullName);
        Assert.Equal("pdf-upload@example.test", guest.EmailAddress);
        Assert.Single(fixture.Email.RegistrationLinks);
    }

    [Fact]
    public async Task CorruptedDocxReturnsStructuredValidationFailure()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var response = await UploadDocxAsync(
            eventDetails.Id, [0x00, 0x01, 0x02], RegistrationHostFixture.PlannerId);

        var result = await JsonAsync(response, HttpStatusCode.BadRequest);

        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal("INVALID_FILE", result.GetProperty("errorCode").GetString());
        Assert.True(result.GetProperty("details").GetArrayLength() > 0);
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
        Assert.Single(fixture.Email.RegistrationLinks);
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
    public async Task RepeatedConcurrentUploadsSendTheRegistrationLinkForEachValidRow()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        const string csv = "fullName,emailAddress\nAlice,concurrent@upload.com\n";
        var responses = await Task.WhenAll(
            UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId),
            UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId));
        var results = await Task.WhenAll(
            responses.Select(response => JsonAsync(response)));

        Assert.All(results, result => Assert.Equal(1, result.GetProperty("successfulRows").GetInt32()));

        await using var db = fixture.CreateDb();
        Assert.Equal(1, await db.Guests.CountAsync(g => g.EventId == eventDetails.Id));
        Assert.Equal(0, await db.RegistrationSubmissions.CountAsync(s => s.EventId == eventDetails.Id));
        Assert.Equal(2, fixture.Email.RegistrationLinks.Count);
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
    public async Task UploadedInviteesEnterAiPipelineOnlyAfterTheySubmit()
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(s => s.CreateFormAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId,
            new Application.GuestManagement.FormSettings(
                RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), 10), Ct));
        await fixture.WithServiceAsync(s => s.PublishAsync(
            eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));

        var form = await fixture.WithServiceAsync(s => s.GetFormAsync(eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));
        var csv = "fullName,emailAddress\nPipeline,pipeline@upload.com\n";
        var upload = await JsonAsync(await UploadCsvAsync(eventDetails.Id, csv, RegistrationHostFixture.PlannerId));
        Assert.Equal(1, upload.GetProperty("successfulRows").GetInt32());

        await using var db = fixture.CreateDb();
        Assert.False(await db.RegistrationSubmissions.AnyAsync(s => s.EventId == eventDetails.Id));
        var uploadedGuest = await db.Guests.SingleAsync(g => g.EventId == eventDetails.Id);
        Assert.Equal("Pipeline", uploadedGuest.FullName);
        Assert.Single(fixture.Email.RegistrationLinks);
        Assert.Contains(form.PublicId!, fixture.Email.RegistrationLinks.Single().RegistrationUrl);

        var receipt = await fixture.WithServiceAsync(s => s.SubmitAsync(form.PublicId!,
            new Application.GuestManagement.GuestDetails("Updated Pipeline", "pipeline@upload.com", "Updated Org", "+94123456789"), Ct));
        Assert.Equal(uploadedGuest.Id, receipt.Registration.GuestId);
        var updatedGuest = await db.Guests.AsNoTracking().SingleAsync(g => g.Id == uploadedGuest.Id);
        Assert.Equal("Updated Pipeline", updatedGuest.FullName);
        Assert.Equal("Updated Org", updatedGuest.Organisation);
        Assert.Equal(RegistrationStatus.PENDING_REVIEW, receipt.Registration.Status);
        Assert.Null(receipt.Registration.Invitation);
    }

    private static TableRow Row(string name, string email)
        => new(
            new TableCell(new Paragraph(new Run(new Text(name)))),
            new TableCell(new Paragraph(new Run(new Text(email)))));

    private static byte[] CreateDocx(params OpenXmlElement[] blocks)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(blocks));
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePdf(string text)
    {
        var content = $"BT /F1 12 Tf 72 720 Td ({text}) Tj ET";
        var contentLength = Encoding.ASCII.GetByteCount(content);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {contentLength} >>\nstream\n{content}\nendstream",
        };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.NewLine = "\n";
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            writer.WriteLine($"{index + 1} 0 obj");
            writer.WriteLine(objects[index]);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        var xrefOffset = stream.Position;
        writer.WriteLine($"xref\n0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine($"startxref\n{xrefOffset}\n%%EOF");
        writer.Flush();
        return stream.ToArray();
    }
}
