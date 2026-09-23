using System.Security.Claims;
using Api.Dtos.Requests;
using Api.Dtos.Responses;
using Api.GuestManagement;
using Application.GuestManagement;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/registration-form")]
[ServiceFilter(typeof(PlannerAccessFilter))]
[ServiceFilter(typeof(RegistrationExceptionFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class RegistrationFormsController(RegistrationService service) : ControllerBase
{
    private string PlannerId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<IActionResult> Create(Guid eventId, RegistrationFormRequest request, CancellationToken ct)
    {
        var form = await service.CreateFormAsync(eventId, PlannerId, request.ToSettings(), ct);
        return CreatedAtAction(nameof(Get), new { eventId }, PlannerFormResponse.From(form));
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid eventId, CancellationToken ct)
        => Ok(PlannerFormResponse.From(await service.GetFormAsync(eventId, PlannerId, ct)));

    [HttpPut]
    public async Task<IActionResult> Update(Guid eventId, RegistrationFormRequest request, CancellationToken ct)
        => Ok(PlannerFormResponse.From(await service.UpdateFormAsync(eventId, PlannerId, request.ToSettings(), ct)));

    [HttpPost("publish")]
    public async Task<IActionResult> Publish(Guid eventId, CancellationToken ct)
        => Ok(PlannerFormResponse.From(await service.PublishAsync(eventId, PlannerId, ct)));

    [HttpPost("question-suggestions")]
    public async Task<IActionResult> SuggestQuestions(Guid eventId,
        [FromServices] IRegistrationQuestionClient client, CancellationToken ct)
        => Ok(await service.SuggestQuestionsAsync(eventId, PlannerId, client, ct));

    [HttpPut("questions")]
    public async Task<IActionResult> SelectQuestions(Guid eventId, SelectRegistrationQuestionsRequest request, CancellationToken ct)
        => Ok(PlannerFormResponse.From(await service.SelectQuestionsAsync(eventId, PlannerId, request.Questions, ct)));

    [HttpPost("registrations/{registrationId:long}/retry-rejection-email")]
    public async Task<IActionResult> RetryRejection(Guid eventId, long registrationId, CancellationToken ct)
        => Ok(PlannerRegistrationResponse.From(await service.RetryRejectionAsync(eventId, PlannerId, registrationId, ct)));

    [HttpPut("seat-limit")]
    public async Task<IActionResult> SeatLimit(Guid eventId, SeatLimitRequest request, CancellationToken ct)
        => Ok(PlannerFormResponse.From(await service.SetSeatLimitAsync(eventId, PlannerId, request.SeatLimit, ct)));

    [HttpGet("registrations")]
    public async Task<IActionResult> List(Guid eventId, CancellationToken ct, int page = 1, int pageSize = 50)
    {
        var result = await service.ListAsync(eventId, PlannerId, page, pageSize, ct);
        return Ok(new { items = result.Items.Select(PlannerRegistrationResponse.From), result.Total, result.Page, result.PageSize });
    }

    [HttpGet("registrations/{registrationId:long}")]
    public async Task<IActionResult> Registration(Guid eventId, long registrationId, CancellationToken ct)
        => Ok(PlannerRegistrationResponse.From(await service.GetRegistrationAsync(eventId, PlannerId, registrationId, ct)));

    [HttpPost("registrations/{registrationId:long}/cancel")]
    public async Task<IActionResult> Cancel(Guid eventId, long registrationId, CancellationToken ct)
        => Ok(PlannerRegistrationResponse.From(await service.CancelAsync(eventId, PlannerId, registrationId, ct)));

    [HttpPost("registrations/{registrationId:long}/retry-invitation")]
    public async Task<IActionResult> Retry(Guid eventId, long registrationId, CancellationToken ct)
        => Ok(PlannerRegistrationResponse.From(await service.RetryInvitationAsync(eventId, PlannerId, registrationId, ct)));

    [HttpPost("invitations/validate")]
    public async Task<IActionResult> ValidateInvitation(Guid eventId, ValidateInvitationRequest request, CancellationToken ct)
        => Ok(new { valid = await service.ValidateInvitationAsync(eventId, PlannerId, request.Token, ct) });

    [HttpGet("registrations/{registrationId:long}/ai-review")]
    public async Task<IActionResult> AiReview(Guid eventId, long registrationId,
        [FromServices] GuestAiReviewService reviews, CancellationToken ct)
        => Ok(GuestAiReviewResponse.From(await reviews.GetAsync(eventId, PlannerId, registrationId, ct)));

    [HttpPost("registrations/{registrationId:long}/retry-ai-review")]
    public async Task<IActionResult> RetryAiReview(Guid eventId, long registrationId,
        [FromServices] GuestAiReviewService reviews, CancellationToken ct)
    {
        var review = await reviews.RetryAsync(eventId, PlannerId, registrationId, ct);
        return AcceptedAtAction(nameof(AiReview), new { eventId, registrationId }, GuestAiReviewResponse.From(review));
    }
}
