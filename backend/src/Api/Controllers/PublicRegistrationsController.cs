using Api.Dtos.Requests;
using Api.Dtos.Responses;
using Api.GuestManagement;
using Application.GuestManagement;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
[ServiceFilter(typeof(RegistrationExceptionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PublicRegistrationsController(RegistrationService service, IRegistrationTokenGenerator tokens, TimeProvider clock) : ControllerBase
{
    [HttpGet("registration-forms/{publicId}")]
    public async Task<IActionResult> GetForm(string publicId, CancellationToken ct)
    {
        var form = await service.GetPublicFormAsync(publicId, ct);
        var now = clock.GetUtcNow();
        return Ok(new PublicFormResponse(form.PublicId!, form.Event.EventName, form.Event.RequirementNotes,
            form.Event.EventStartDate, form.Event.EventEndDate, form.Event.PreferredLocation, form.OpensAt,
            form.ClosesAt, form.SeatLimit, now >= form.OpensAt && now < form.ClosesAt && now < form.Event.EventEndDate,
            ["fullName", "emailAddress"], ["organisation", "phoneNumber"], FormQuestionResponse.From(form)));
    }

    [HttpPost("registration-forms/{publicId}/registrations")]
    public async Task<IActionResult> Submit(string publicId, SubmitRegistrationRequest request, CancellationToken ct)
    {
        var receipt = await service.SubmitAsync(publicId, request.ToDetails(), request.Answers, ct);
        return StatusCode(StatusCodes.Status201Created, ToResponse(receipt.Registration, receipt.StatusSecret));
    }

    [HttpPost("registrations/{publicReference}/status")]
    public async Task<IActionResult> Status(string publicReference, RegistrationAccessRequest request, CancellationToken ct)
        => Ok(ToResponse(await service.GetPublicStatusAsync(publicReference, request.Secret, ct)));

    [HttpPost("registrations/{publicReference}/rsvp")]
    public async Task<IActionResult> Rsvp(string publicReference, RegistrationRsvpRequest request, CancellationToken ct)
    {
        if (!Enum.GetNames<RsvpStatus>().Contains(request.Response) || !Enum.TryParse<RsvpStatus>(request.Response, out var response))
            throw new RegistrationException(400, "invalid_rsvp", "RSVP must be ACCEPTED, DECLINED, or MAYBE.");
        return Ok(ToResponse(await service.RespondAsync(publicReference, request.Secret, response, ct)));
    }

    private PublicRegistrationResponse ToResponse(RegistrationSubmission registration, string? secret = null)
    {
        var token = service.IsActive(registration) ? registration.Invitation!.Token : null;
        return new PublicRegistrationResponse(registration.PublicReference, registration.Status.ToString(), secret,
            token, token is null ? null : Convert.ToBase64String(tokens.CreateQrPng(token)),
            registration.Invitation?.RsvpStatus.ToString(),
            registration.Status == RegistrationStatus.REJECTED ? registration.RejectionDeliveryStatus?.ToString() : registration.Invitation?.DeliveryStatus.ToString(),
            registration.RegisteredAt, registration.ConfirmedAt, registration.CancelledAt);
    }
}
