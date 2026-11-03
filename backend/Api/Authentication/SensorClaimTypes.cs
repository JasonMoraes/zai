namespace Api.Authentication;

/// <summary>Claims of a sensor principal created by <see cref="ApiKeyAuthenticationHandler"/>.</summary>
public static class SensorClaimTypes
{
    public const string SensorId = "sensor_id";
    public const string SeriesId = "series_id";
}
