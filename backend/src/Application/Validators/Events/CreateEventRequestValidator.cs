using Application.Dtos.Events;
using FluentValidation;

namespace Application.Validators.Events;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(request => request.EventName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.EventType)
            .NotNull()
            .IsInEnum();

        RuleFor(request => request.GuestCount)
            .GreaterThan(0);

        RuleFor(request => request.Budget)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.PreferredVenue)
            .NotEmpty();

        RuleFor(request => ResolveEventDate(request))
            .GreaterThan(_ => DateTime.UtcNow)
            .WithName(nameof(CreateEventRequest.PreferredDate));

        RuleFor(request => request.StartTime)
            .NotNull();

        RuleFor(request => request.EndTime)
            .NotNull();

        RuleFor(request => request)
            .Must(request => !request.StartTime.HasValue
                || !request.EndTime.HasValue
                || request.EndTime.Value > request.StartTime.Value)
            .WithName(nameof(CreateEventRequest.EndTime))
            .WithMessage("EndTime must be strictly after StartTime.");
    }

    private static DateTime ResolveEventDate(CreateEventRequest request) =>
        request.EventDate ?? request.PreferredDate;
}
