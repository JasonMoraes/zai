namespace Api.Entities;

public class Measurement
{
    public long Id { get; set; }
    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    /// <summary>The sensor that sent the result; <c>null</c> once that sensor is unregistered (the result stays).</summary>
    public int? SensorId { get; set; }
    public Sensor? Sensor { get; set; }
    public double Value { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
