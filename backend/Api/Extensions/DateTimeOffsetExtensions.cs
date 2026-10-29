namespace Api.Extensions;

public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// PostgreSQL timestamps hold microseconds, .NET ticks are 100 ns. Truncating before saving makes the value
    /// returned on create identical to what later reads return.
    /// </summary>
    public static DateTimeOffset TruncateToMicroseconds(this DateTimeOffset value) =>
        value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));
}
