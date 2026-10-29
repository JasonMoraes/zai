using Api.Entities;

namespace Api.Dtos.Sensors;

/// <summary>A sensor as returned by every endpoint except registration: never includes the API key.</summary>
public record SensorResponse(int Id, string Name, int SeriesId, DateTimeOffset CreatedAt, DateTimeOffset? LastMeasurementAt)
{
    public static SensorResponse From(Sensor sensor) =>
        new(sensor.Id, sensor.Name, sensor.SeriesId, sensor.CreatedAt, sensor.LastMeasurementAt);
}

/// <summary>Registration response: the only time the plain API key is ever disclosed.</summary>
public record SensorCreatedResponse(
    int Id, string Name, int SeriesId, DateTimeOffset CreatedAt, DateTimeOffset? LastMeasurementAt, string ApiKey)
    : SensorResponse(Id, Name, SeriesId, CreatedAt, LastMeasurementAt)
{
    public static SensorCreatedResponse From(Sensor sensor, string apiKey) =>
        new(sensor.Id, sensor.Name, sensor.SeriesId, sensor.CreatedAt, sensor.LastMeasurementAt, apiKey);
}
