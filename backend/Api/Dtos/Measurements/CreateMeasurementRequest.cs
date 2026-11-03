using System.ComponentModel.DataAnnotations;
using Api.Validation;

namespace Api.Dtos.Measurements;

/// <summary>The series is not part of the body: it comes from the sensor's API key.</summary>
public record CreateMeasurementRequest
{
    [Required(ErrorMessage = "Pole jest wymagane.")]
    [FiniteNumber]
    public double? Value { get; init; }

    /// <summary>ISO 8601 with a zone; omitted means the server's current time. Kept a string so it is parsed strictly.</summary>
    [IsoDateTime]
    public string? Timestamp { get; init; }

    /// <summary>
    /// The parsed timestamp, <c>null</c> when omitted. Throws for a malformed value rather than quietly turning it into
    /// "now": [IsoDateTime] must already have rejected it with 400.
    /// </summary>
    public DateTimeOffset? ParsedTimestamp() =>
        Timestamp is null ? null
        : IsoDateTime.TryParse(Timestamp, out var value) ? value
        : throw new InvalidOperationException("Timestamp must be validated before it is used.");
}
