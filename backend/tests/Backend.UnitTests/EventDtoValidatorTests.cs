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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ReturnsError_WhenEventNameIsMissing(string eventName)
    {
        var request = ValidRequest();
        request.EventName = eventName;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.EventName));
    }

    [Fact]
    public void Validate_ReturnsError_WhenEventNameIsTooLong()
    {
        var request = ValidRequest();
        request.EventName = new string('x', 201);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.EventName));
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

    [Fact]
    public void Validate_IgnoresClientSuppliedEventDuration()
    {
        var request = ValidRequest();
        request.EventDuration = TimeSpan.Zero;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsError_WhenStartTimeIsMissing()
    {
        var request = ValidRequest();
        request.StartTime = null;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.StartTime));
    }

    [Fact]
    public void Validate_ReturnsError_WhenEndTimeIsMissing()
    {
        var request = ValidRequest();
        request.EndTime = null;

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.EndTime));
    }

    [Fact]
    public void Validate_ReturnsError_WhenStartAndEndTimeAreEqual()
    {
        var request = ValidRequest();
        request.StartTime = new TimeOnly(18, 0);
        request.EndTime = new TimeOnly(18, 0);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.EndTime));
    }

    [Fact]
    public void Validate_ReturnsError_WhenEndTimeIsBeforeStartTime()
    {
        var request = ValidRequest();
        request.StartTime = new TimeOnly(23, 0);
        request.EndTime = new TimeOnly(18, 0);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateEventRequest.EndTime));
    }

    private static CreateEventRequest ValidRequest() => new()
    {
        EventName = "Annual celebration",
        EventType = EventType.WEDDING,
        GuestCount = 100,
        Budget = 5000,
        PreferredVenue = "Grand Hall",
        PreferredDate = DateTime.UtcNow.AddDays(30),
        StartTime = new TimeOnly(18, 0),
        EndTime = new TimeOnly(23, 0),
        EventDuration = TimeSpan.FromHours(4)
    };
}
