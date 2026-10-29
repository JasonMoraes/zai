namespace Api.Entities;

public class Sensor
{
    /// <summary>Hex-encoded SHA-256 of the API key; the key itself is never stored.</summary>
    public const int ApiKeyHashLength = 64;

    public int Id { get; set; }
    public required string Name { get; set; }
    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    public required string ApiKeyHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastMeasurementAt { get; set; }
}
