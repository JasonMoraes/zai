namespace Api.Services;

public static class MeasurementRules
{
    /// <summary>Contract: timestamps later than this past the server's clock are rejected.</summary>
    public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);
}
