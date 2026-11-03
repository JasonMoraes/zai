using Api.Entities;

namespace Api.Services;

public enum MeasurementRuleViolation
{
    ValueOutOfRange,
    TimestampTooFarInFuture,
}

/// <summary>
/// Outcome of an upload: the saved measurement, the business rules it broke (422), or
/// <see cref="SensorGone"/> when the sensor was unregistered mid-request (401).
/// </summary>
public record CreateMeasurementResult(
    Measurement? Measurement,
    Series? Series,
    IReadOnlyList<MeasurementRuleViolation> Violations,
    bool SensorGone = false)
{
    public static CreateMeasurementResult Gone() => new(null, null, [], SensorGone: true);
}
