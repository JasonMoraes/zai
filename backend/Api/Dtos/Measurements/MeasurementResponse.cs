using System.Text.Json.Serialization;
using Api.Entities;

namespace Api.Dtos.Measurements;

/// <param name="SensorId">
/// Omitted (not <c>null</c>) once the sensor is unregistered: the contract types it as a non-nullable optional id.
/// </param>
public record MeasurementResponse(
    long Id,
    int SeriesId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? SensorId,
    double Value,
    DateTimeOffset Timestamp)
{
    public static MeasurementResponse From(Measurement measurement) => new(
        measurement.Id, measurement.SeriesId, measurement.SensorId, measurement.Value, measurement.Timestamp.ToUniversalTime());
}
