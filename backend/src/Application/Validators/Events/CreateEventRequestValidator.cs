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

        RuleFor(request => request.PreferredDate)
            .GreaterThan(_ => DateTime.UtcNow);

        RuleFor(request => request.EventDuration)
            .GreaterThan(TimeSpan.Zero);
    }
}
