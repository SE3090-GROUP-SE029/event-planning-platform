using Application.Dtos.Events;
using Application.Validators.Events;
using Domain.Enums;

namespace Backend.UnitTests;

public class EventDtoValidatorTests
{
    private readonly CreateEventRequestValidator _validator = new();

    [Fact]
    public void Validate_ReturnsValid_ForValidRequest()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsError_WhenEventTypeIsMissing()
    {
        var request = ValidRequest();
        request.EventType = null;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.EventType));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReturnsError_WhenGuestCountIsNotPositive(int guestCount)
    {
        var request = ValidRequest();
        request.GuestCount = guestCount;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.GuestCount));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Validate_ReturnsError_WhenBudgetIsNegative(double budget)
    {
        var request = ValidRequest();
        request.Budget = (decimal)budget;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.Budget));
    }

    [Fact]
    public void Validate_ReturnsError_WhenPreferredVenueIsMissing()
    {
        var request = ValidRequest();
        request.PreferredVenue = " ";

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.PreferredVenue));
    }

    [Fact]
    public void Validate_ReturnsError_WhenPreferredDateIsNotInTheFuture()
    {
        var request = ValidRequest();
        request.PreferredDate = DateTime.UtcNow;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.PreferredDate));
    }

    private static CreateEventRequest ValidRequest() => new()
    {
        EventType = EventType.WEDDING,
        GuestCount = 100,
        Budget = 5000,
        PreferredVenue = "Grand Hall",
        PreferredDate = DateTime.UtcNow.AddDays(30),
        EventDuration = TimeSpan.FromHours(4)
    };
}
