namespace Application.Common;

public static class UtcDateTime
{
    public static DateTime Normalize(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    public static DateTimeOffset Normalize(DateTimeOffset value) =>
        value.ToUniversalTime();

    public static DateTime ToExclusiveUpperBound(DateTime value) =>
        value.TimeOfDay == TimeSpan.Zero ? value.AddDays(1) : value;
}
