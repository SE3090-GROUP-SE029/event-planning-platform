using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.GuestManagement;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.ExternalServices;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Backend.IntegrationTests;

public class RegistrationEndpointTests(RegistrationHostFixture fixture) : IClassFixture<RegistrationHostFixture>
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static FormSettings Settings(int seats = 2) => new(RegistrationHostFixture.Now.AddHours(-1), RegistrationHostFixture.Now.AddDays(5), seats);
    private static string PlannerPath(Guid eventId) => $"/api/events/{eventId}/registration-form";
    private static string PublicPath(string publicId) => $"/api/public/registration-forms/{publicId}";
    private static object GuestBody(int number) => new { fullName = $"Guest {number}", emailAddress = $"guest{number}@example.com", organisation = "SLIIT", phoneNumber = "+94 123456789" };

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null, string? planner = null, string? role = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (planner is not null) request.Headers.Add("X-Test-Identity", planner);
        if (role is not null) request.Headers.Add("X-Test-Role", role);
        return await fixture.Client.SendAsync(request);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}; got {response.StatusCode}: {text}");
        using var json = JsonDocument.Parse(text);
        return json.RootElement.Clone();
    }

    private async Task<(Event Event, string PublicId)> PublishedAsync(int seats = 2, FormSettings? settings = null)
    {
        var eventDetails = await fixture.CreateEventAsync();
        await fixture.WithServiceAsync(service => service.CreateFormAsync(eventDetails.Id, RegistrationHostFixture.PlannerId, settings ?? Settings(seats), Ct));
        var form = await fixture.WithServiceAsync(service => service.PublishAsync(eventDetails.Id, RegistrationHostFixture.PlannerId, Ct));
        return (eventDetails, form.PublicId!);
    }

    private async Task<JsonElement> SubmitAsync(string publicId, int number)
    {
        var receipt = await JsonAsync(await SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", GuestBody(number)), HttpStatusCode.Created);
        Assert.Equal("PENDING_AI", receipt.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, receipt.GetProperty("invitationToken").ValueKind);
        await fixture.DrainAiAsync();
        var result = System.Text.Json.Nodes.JsonNode.Parse((await StatusAsync(receipt)).GetRawText())!;
        result["statusSecret"] = receipt.GetProperty("statusSecret").GetString();
        return JsonSerializer.SerializeToElement(result);
    }

    private async Task<JsonElement> StatusAsync(JsonElement receipt)
        => await JsonAsync(await SendAsync(HttpMethod.Post, $"/api/public/registrations/{receipt.GetProperty("publicReference").GetString()}/status",
            new { secret = receipt.GetProperty("statusSecret").GetString() }));

    [Fact]
    public async Task CreateUpdatePublishAndAnonymousPublicAccess()
    {
        var eventDetails = await fixture.CreateEventAsync();
        var created = await JsonAsync(await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id), Settings(), RegistrationHostFixture.PlannerId), HttpStatusCode.Created);
        Assert.Equal("DRAFT", created.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("publicId").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, PublicPath(new RegistrationTokenGenerator().Generate()))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id), Settings(), RegistrationHostFixture.PlannerId)).StatusCode);
        var updated = await JsonAsync(await SendAsync(HttpMethod.Put, PlannerPath(eventDetails.Id), Settings(3), RegistrationHostFixture.PlannerId));
        Assert.Equal(3, updated.GetProperty("seatLimit").GetInt32());
        var published = await JsonAsync(await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id) + "/publish", planner: RegistrationHostFixture.PlannerId));
        var repeated = await JsonAsync(await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id) + "/publish", planner: RegistrationHostFixture.PlannerId));
        Assert.Equal(published.GetProperty("publicId").GetString(), repeated.GetProperty("publicId").GetString());
        var publicForm = await JsonAsync(await SendAsync(HttpMethod.Get, published.GetProperty("publicPath").GetString()!));
        Assert.Equal(eventDetails.EventName, publicForm.GetProperty("eventName").GetString());
        Assert.Equal("Test description", publicForm.GetProperty("description").GetString());
        Assert.Equal("Colombo", publicForm.GetProperty("location").GetString());
        Assert.True(publicForm.GetProperty("isOpen").GetBoolean());
        Assert.False(publicForm.TryGetProperty("eventId", out _));
        Assert.False(publicForm.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task ValidRegistrationsConfirmUntilCapacityThenWaitWithoutQrOrEmail()
    {
        var (eventDetails, publicId) = await PublishedAsync(2);
        var first = await SubmitAsync(publicId, 1);
        var second = await SubmitAsync(publicId, 2);
        var third = await SubmitAsync(publicId, 3);
        Assert.Equal("CONFIRMED", first.GetProperty("status").GetString());
        Assert.Equal("CONFIRMED", second.GetProperty("status").GetString());
        Assert.NotEqual(first.GetProperty("invitationToken").GetString(), second.GetProperty("invitationToken").GetString());
        Assert.NotEqual(first.GetProperty("statusSecret").GetString(), first.GetProperty("invitationToken").GetString());
        Assert.Equal("SENT", first.GetProperty("emailDeliveryStatus").GetString());
        var png = Convert.FromBase64String(first.GetProperty("qrPngBase64").GetString()!);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.Equal("WAITING_LIST", third.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, third.GetProperty("invitationToken").ValueKind);
        Assert.Equal(JsonValueKind.Null, third.GetProperty("qrPngBase64").ValueKind);
        Assert.False(first.TryGetProperty("id", out _));
        Assert.False(first.TryGetProperty("guestId", out _));
        await using var db = fixture.CreateDb();
        Assert.Equal(3, await db.RegistrationSubmissions.CountAsync(r => r.EventId == eventDetails.Id));
        Assert.Equal(2, await db.Invitations.CountAsync(i => i.RegistrationSubmission.EventId == eventDetails.Id));
        var saved = await db.RegistrationSubmissions.SingleAsync(r => r.PublicReference == first.GetProperty("publicReference").GetString());
        Assert.NotEqual(first.GetProperty("statusSecret").GetString(), saved.StatusSecretHash);
        Assert.Contains(fixture.Email.Messages, m => m.Token == first.GetProperty("invitationToken").GetString());
    }

    [Theory]
    [InlineData("", "guest@example.com")]
    [InlineData("   ", "guest@example.com")]
    [InlineData("Guest", "invalid")]
    [InlineData("Guest", "")]
    public async Task InvalidPublicSubmissionsReturn400AndDoNotPersist(string name, string email)
    {
        var (eventDetails, publicId) = await PublishedAsync();
        var response = await SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", new { fullName = name, emailAddress = email });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.CreateDb();
        Assert.False(await db.Guests.AnyAsync(g => g.EventId == eventDetails.Id));
    }

    [Fact]
    public async Task RegistrationWindowIncludesOpeningAndExcludesClosing()
    {
        var opens = RegistrationHostFixture.Now.AddHours(1);
        var closes = opens.AddHours(1);
        var (_, publicId) = await PublishedAsync(settings: new FormSettings(opens, closes, 5));
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", GuestBody(1))).StatusCode);
        fixture.Clock.UtcNow = opens;
        await SubmitAsync(publicId, 1);
        fixture.Clock.UtcNow = closes;
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", GuestBody(2))).StatusCode);
        fixture.Clock.UtcNow = closes.AddSeconds(1);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", GuestBody(3))).StatusCode);
        var form = await JsonAsync(await SendAsync(HttpMethod.Get, PublicPath(publicId)));
        Assert.False(form.GetProperty("isOpen").GetBoolean());
    }

    [Fact]
    public async Task DuplicateEmailIsNormalizedAndScopedToEvent()
    {
        var (_, publicId) = await PublishedAsync();
        await SubmitAsync(publicId, 1);
        var duplicate = await SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations",
            new { fullName = "Other Name", emailAddress = " GUEST1@EXAMPLE.COM " });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var (_, otherId) = await PublishedAsync();
        await SubmitAsync(otherId, 1);
    }

    [Fact]
    public async Task TwoHundredRegistrationsWithOneHundredSeats()
    {
        var (eventDetails, publicId) = await PublishedAsync(100);
        for (var index = 0; index < 200; index++)
        {
            var receipt = await fixture.WithServiceAsync(service => service.SubmitAsync(publicId,
                new GuestDetails($"Guest {index}", $"scale{index}@example.com", null, null), Ct));
            receipt = receipt with { Registration = await fixture.AcceptAsync(receipt) };
            Assert.Equal(index < 100 ? RegistrationStatus.CONFIRMED : RegistrationStatus.WAITING_LIST, receipt.Registration.Status);
            Assert.Equal(index < 100, receipt.Registration.Invitation is not null);
        }
        await using var db = fixture.CreateDb();
        Assert.Equal(200, await db.RegistrationSubmissions.CountAsync(r => r.EventId == eventDetails.Id));
        Assert.Equal(100, await db.Invitations.CountAsync(i => i.RegistrationSubmission.EventId == eventDetails.Id));
    }

    [Fact]
    public async Task ConcurrentHttpSubmissionsCannotOverbook()
    {
        var (eventDetails, publicId) = await PublishedAsync(3);
        var replies = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", GuestBody(i))));
        var rows = await Task.WhenAll(replies.Select(r => JsonAsync(r, HttpStatusCode.Created)));
        Assert.All(rows, r => Assert.Equal("PENDING_AI", r.GetProperty("status").GetString()));
        await fixture.DrainAiAsync();
        rows = await Task.WhenAll(rows.Select(StatusAsync));
        Assert.Equal(3, rows.Count(r => r.GetProperty("status").GetString() == "CONFIRMED"));
        Assert.Equal(17, rows.Count(r => r.GetProperty("status").GetString() == "WAITING_LIST"));
        await using var db = fixture.CreateDb();
        Assert.Equal(3, await db.Invitations.CountAsync(i => i.RegistrationSubmission.EventId == eventDetails.Id));
    }

    [Fact]
    public async Task ConcurrentDuplicateSubmissionsCreateOnlyOneGuest()
    {
        var (eventDetails, publicId) = await PublishedAsync(10);
        var replies = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => SendAsync(HttpMethod.Post, PublicPath(publicId) + "/registrations", GuestBody(1))));
        Assert.Single(replies, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(7, replies.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        await using var db = fixture.CreateDb();
        Assert.Equal(1, await db.Guests.CountAsync(g => g.EventId == eventDetails.Id));
    }

    [Fact]
    public async Task CancellationPromotesInOrderAndRevokesOldQrIdempotently()
    {
        var (eventDetails, publicId) = await PublishedAsync(1);
        var first = await SubmitAsync(publicId, 1);
        var second = await SubmitAsync(publicId, 2);
        var third = await SubmitAsync(publicId, 3);
        var page = await JsonAsync(await SendAsync(HttpMethod.Get, PlannerPath(eventDetails.Id) + "/registrations", planner: RegistrationHostFixture.PlannerId));
        var firstId = page.GetProperty("items")[0].GetProperty("id").GetInt64();
        var path = PlannerPath(eventDetails.Id) + $"/registrations/{firstId}/cancel";
        await JsonAsync(await SendAsync(HttpMethod.Post, path, planner: RegistrationHostFixture.PlannerId));
        await JsonAsync(await SendAsync(HttpMethod.Post, path, planner: RegistrationHostFixture.PlannerId));
        var firstStatus = await StatusAsync(first);
        var secondStatus = await StatusAsync(second);
        var thirdStatus = await StatusAsync(third);
        Assert.Equal("CANCELLED", firstStatus.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, firstStatus.GetProperty("invitationToken").ValueKind);
        Assert.Equal("CONFIRMED", secondStatus.GetProperty("status").GetString());
        Assert.Equal("SENT", secondStatus.GetProperty("emailDeliveryStatus").GetString());
        Assert.Equal("WAITING_LIST", thirdStatus.GetProperty("status").GetString());
        var invalid = await JsonAsync(await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id) + "/invitations/validate",
            new { token = first.GetProperty("invitationToken").GetString() }, RegistrationHostFixture.PlannerId));
        var valid = await JsonAsync(await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id) + "/invitations/validate",
            new { token = secondStatus.GetProperty("invitationToken").GetString() }, RegistrationHostFixture.PlannerId));
        Assert.False(invalid.GetProperty("valid").GetBoolean());
        Assert.True(valid.GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task CapacityIncreasePromotesAndDecreaseCannotDemoteConfirmedGuests()
    {
        var (eventDetails, publicId) = await PublishedAsync(1);
        await SubmitAsync(publicId, 1);
        var second = await SubmitAsync(publicId, 2);
        var third = await SubmitAsync(publicId, 3);
        await JsonAsync(await SendAsync(HttpMethod.Put, PlannerPath(eventDetails.Id) + "/seat-limit", new { seatLimit = 2 }, RegistrationHostFixture.PlannerId));
        Assert.Equal("CONFIRMED", (await StatusAsync(second)).GetProperty("status").GetString());
        Assert.Equal("WAITING_LIST", (await StatusAsync(third)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Put, PlannerPath(eventDetails.Id) + "/seat-limit", new { seatLimit = 1 }, RegistrationHostFixture.PlannerId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Put, PlannerPath(eventDetails.Id) + "/seat-limit", new { seatLimit = 0 }, RegistrationHostFixture.PlannerId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Put, PlannerPath(eventDetails.Id), Settings(1), RegistrationHostFixture.PlannerId)).StatusCode);
    }

    [Fact]
    public async Task RsvpCannotBypassCapacityAndDeclineReleasesSeat()
    {
        var (_, publicId) = await PublishedAsync(1);
        var first = await SubmitAsync(publicId, 1);
        var second = await SubmitAsync(publicId, 2);
        string Path(JsonElement r) => $"/api/public/registrations/{r.GetProperty("publicReference").GetString()}/rsvp";
        object Body(JsonElement r, string response) => new { secret = r.GetProperty("statusSecret").GetString(), response };
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, Path(second), Body(second, "ACCEPTED"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, Path(first), Body(first, "ATTENDED"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, Path(first), Body(first, "1"))).StatusCode);
        var accepted = await JsonAsync(await SendAsync(HttpMethod.Post, Path(first), Body(first, "ACCEPTED")));
        Assert.Equal("ACCEPTED", accepted.GetProperty("rsvpStatus").GetString());
        await JsonAsync(await SendAsync(HttpMethod.Post, Path(first), Body(first, "DECLINED")));
        Assert.Equal("CONFIRMED", (await StatusAsync(second)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, Path(first), Body(first, "ACCEPTED"))).StatusCode);
    }

    [Fact]
    public async Task StatusNeedsSeparateSecretAndResponseIsNotCached()
    {
        var (_, publicId) = await PublishedAsync();
        var receipt = await SubmitAsync(publicId, 1);
        var path = $"/api/public/registrations/{receipt.GetProperty("publicReference").GetString()}/status";
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Post, path, new { secret = new RegistrationTokenGenerator().Generate() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, path, new { })).StatusCode);
        var response = await SendAsync(HttpMethod.Post, path, new { secret = receipt.GetProperty("statusSecret").GetString() });
        Assert.True(response.Headers.CacheControl?.NoStore);
        var status = await JsonAsync(response);
        Assert.Equal(JsonValueKind.Null, status.GetProperty("statusSecret").ValueKind);
    }

    [Fact]
    public async Task PlannerEndpointsDenyAnonymousWrongRoleAndWrongOwner()
    {
        var (eventDetails, _) = await PublishedAsync();
        var path = PlannerPath(eventDetails.Id);
        foreach (var suffix in new[] { "", "/registrations", "/registrations/1" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, path + suffix)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, path + suffix, planner: RegistrationHostFixture.PlannerId, role: "Guest")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, path + suffix, planner: RegistrationHostFixture.GuestOwnerGuid("someone-else").ToString())).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Post, path + "/publish")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Post, path + "/registrations/1/cancel")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Get, path + "/registrations?page=0", planner: RegistrationHostFixture.PlannerId)).StatusCode);
    }

    [Fact]
    public async Task EmailFailureRetainsConfirmationAndRetryKeepsToken()
    {
        var (eventDetails, publicId) = await PublishedAsync();
        fixture.Email.ThrowOnSend = true;
        var receipt = await SubmitAsync(publicId, 1);
        Assert.Equal("CONFIRMED", receipt.GetProperty("status").GetString());
        Assert.Equal("FAILED", receipt.GetProperty("emailDeliveryStatus").GetString());
        var list = await JsonAsync(await SendAsync(HttpMethod.Get, PlannerPath(eventDetails.Id) + "/registrations", planner: RegistrationHostFixture.PlannerId));
        var id = list.GetProperty("items")[0].GetProperty("id").GetInt64();
        fixture.Email.ThrowOnSend = false;
        var path = PlannerPath(eventDetails.Id) + $"/registrations/{id}/retry-invitation";
        var retried = await JsonAsync(await SendAsync(HttpMethod.Post, path, planner: RegistrationHostFixture.PlannerId));
        Assert.Equal("SENT", retried.GetProperty("emailDeliveryStatus").GetString());
        Assert.Equal(2, retried.GetProperty("deliveryAttempts").GetInt32());
        var repeated = await JsonAsync(await SendAsync(HttpMethod.Post, path, planner: RegistrationHostFixture.PlannerId));
        Assert.Equal(2, repeated.GetProperty("deliveryAttempts").GetInt32());
        Assert.Equal(receipt.GetProperty("invitationToken").GetString(), (await StatusAsync(receipt)).GetProperty("invitationToken").GetString());
    }

    [Fact]
    public async Task MissingEmailConfigurationLeavesDeliveryPending()
    {
        var (_, publicId) = await PublishedAsync();
        fixture.Email.Result = EmailDeliveryResult.UNAVAILABLE;
        var receipt = await SubmitAsync(publicId, 1);
        Assert.Equal("CONFIRMED", receipt.GetProperty("status").GetString());
        Assert.Equal("PENDING", receipt.GetProperty("emailDeliveryStatus").GetString());
    }

    [Fact]
    public async Task ExpiredInvitationCannotValidateOrAcceptRsvp()
    {
        var (eventDetails, publicId) = await PublishedAsync();
        var receipt = await SubmitAsync(publicId, 1);
        fixture.Clock.UtcNow = eventDetails.EventEndDate;
        var status = await StatusAsync(receipt);
        Assert.Equal(JsonValueKind.Null, status.GetProperty("invitationToken").ValueKind);
        var response = await JsonAsync(await SendAsync(HttpMethod.Post, PlannerPath(eventDetails.Id) + "/invitations/validate",
            new { token = receipt.GetProperty("invitationToken").GetString() }, RegistrationHostFixture.PlannerId));
        Assert.False(response.GetProperty("valid").GetBoolean());
    }

    [Fact]
    public async Task EligibilityExtensionCanKeepGuestWaitingWithoutBlockingNextEligibleGuest()
    {
        var (eventDetails, publicId) = await PublishedAsync(1);
        await using var db = fixture.CreateDb();
        var service = new RegistrationService(new GuestRegistrationRepository(db), new RegistrationTokenGenerator(), new HoldPolicy(), fixture.Email, fixture.Clock);
        var held = await service.SubmitAsync(publicId, new GuestDetails("Hold", "hold@example.com", null, null), Ct);
        var eligible = await service.SubmitAsync(publicId, new GuestDetails("Eligible", "eligible@example.com", null, null), Ct);
        var reviews = new GuestAiReviewRepository(db, new GuestRegistrationRepository(db),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GuestAiReviewRepository>.Instance);
        for (var i = 0; i < 2; i++)
        {
            var claim = await reviews.ClaimAsync(fixture.Clock.GetUtcNow(), fixture.Clock.GetUtcNow().AddMinutes(3), Ct);
            Assert.NotNull(claim);
            await service.ApplyDecisionAsync(claim!, fixture.Ai.Decision, reviews, Ct);
        }
        held = held with { Registration = (await service.GetPublicStatusAsync(held.Registration.PublicReference, held.StatusSecret, Ct)) };
        eligible = eligible with { Registration = (await service.GetPublicStatusAsync(eligible.Registration.PublicReference, eligible.StatusSecret, Ct)) };
        Assert.Equal(RegistrationStatus.WAITING_LIST, held.Registration.Status);
        Assert.Null(held.Registration.Invitation);
        Assert.Equal(RegistrationStatus.CONFIRMED, eligible.Registration.Status);
    }

    [Fact]
    public async Task MigrationsMatchModelAndDatabaseConstraintsAreEnforced()
    {
        var (eventDetails, publicId) = await PublishedAsync();
        await SubmitAsync(publicId, 1);
        await using var db = fixture.CreateDb();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        var migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Contains("20260914055022_InitialCreate", migrations);
        Assert.Contains(migrations, m => m.EndsWith("_AddGuestRegistrationFlow1"));
        db.Guests.Add(new Guest { EventId = eventDetails.Id, FullName = "Duplicate", EmailAddress = "GUEST1@example.com", NormalizedEmail = "GUEST1@EXAMPLE.COM" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DatabaseRejectsNonpositiveCapacityAndCrossEventAssociations()
    {
        var (eventDetails, publicId) = await PublishedAsync();
        var otherEvent = await fixture.CreateEventAsync();
        await using (var db = fixture.CreateDb())
        {
            db.RegistrationForms.Add(new RegistrationForm { EventId = otherEvent.Id, OpensAt = RegistrationHostFixture.Now, ClosesAt = RegistrationHostFixture.Now.AddDays(1), SeatLimit = 0 });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = fixture.CreateDb())
        {
            var form = await db.RegistrationForms.SingleAsync(f => f.EventId == eventDetails.Id);
            var guest = new Guest { EventId = otherEvent.Id, FullName = "Guest", EmailAddress = "cross@example.com", NormalizedEmail = "CROSS@EXAMPLE.COM" };
            db.Guests.Add(guest);
            await db.SaveChangesAsync();
            db.RegistrationSubmissions.Add(new RegistrationSubmission
            {
                EventId = eventDetails.Id, RegistrationFormId = form.Id, GuestId = guest.Id,
                PublicReference = new RegistrationTokenGenerator().Generate(), StatusSecretHash = new string('A', 64)
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task ExistingTestFeaturePingCreateAndValidationStillWork()
    {
        var ping = await JsonAsync(await SendAsync(HttpMethod.Get, "/api/Test/ping"));
        Assert.Equal("pong", ping.GetProperty("message").GetString());
        var message = await JsonAsync(await SendAsync(HttpMethod.Post, "/api/Test/message", new { message = "Regression check" }), HttpStatusCode.Created);
        Assert.Equal("Regression check", message.GetProperty("message").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/Test/message", new { message = "" })).StatusCode);
    }

    private class HoldPolicy : IRegistrationEligibilityPolicy
    {
        public Task<bool> IsEligibleAsync(Guest guest, Event eventDetails, CancellationToken cancellationToken) => Task.FromResult(guest.FullName != "Hold");
    }
}
