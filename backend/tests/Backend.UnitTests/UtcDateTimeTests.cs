using Application.Common;

namespace Backend.UnitTests;

public sealed class UtcDateTimeTests
{
    [Fact]
    public void Normalize_marks_unspecified_values_as_utc_without_changing_ticks()
    {
        var input = new DateTime(2026, 10, 4, 12, 30, 0, DateTimeKind.Unspecified);

        var result = UtcDateTime.Normalize(input);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(input.Ticks, result.Ticks);
    }

    [Fact]
    public void Normalize_converts_local_values_to_utc()
    {
        var input = new DateTime(2026, 10, 4, 12, 30, 0, DateTimeKind.Local);

        var result = UtcDateTime.Normalize(input);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(input.ToUniversalTime().Ticks, result.Ticks);
    }

    [Fact]
    public void Normalize_converts_offset_values_to_zero_offset()
    {
        var input = new DateTimeOffset(2026, 10, 4, 12, 30, 0, TimeSpan.FromHours(5.5));

        var result = UtcDateTime.Normalize(input);

        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(input.UtcTicks, result.UtcTicks);
    }

    [Fact]
    public void ToExclusiveUpperBound_includes_the_entire_selected_day()
    {
        var selectedDate = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc);

        var result = UtcDateTime.ToExclusiveUpperBound(selectedDate);

        Assert.Equal(selectedDate.AddDays(1), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }
}
