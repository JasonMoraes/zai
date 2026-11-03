using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Api.Extensions;
using Api.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Dtos.Measurements;

/// <summary>Query parameters of <c>GET /api/measurements</c>; invalid values give 400.</summary>
public record MeasurementQuery : IValidatableObject
{
    public const int DefaultLimit = 1000;
    public const int MaxLimit = 10000;

    /// <summary>Comma-separated series ids, e.g. "1,3"; absent means all series.</summary>
    [FromQuery(Name = "series")]
    public string? Series { get; init; }

    [FromQuery(Name = "from")]
    public string? From { get; init; }

    [FromQuery(Name = "to")]
    public string? To { get; init; }

    [FromQuery(Name = "sort")]
    [RegularExpression("^-?timestamp$", ErrorMessage = "Dozwolone wartości: timestamp, -timestamp.")]
    public string? Sort { get; init; }

    [FromQuery(Name = "limit")]
    [Range(1, MaxLimit, ErrorMessage = "Wartość musi należeć do przedziału {1}–{2}.")]
    public int? Limit { get; init; }

    // Methods rather than properties, so model binding and Swagger don't treat them as query parameters.
    public bool IsDescending() => Sort == "-timestamp";

    public int EffectiveLimit() => Limit ?? DefaultLimit;

    /// <summary><c>null</c> when the parameter is absent, meaning all series (not an empty list, which matches none).</summary>
    public IReadOnlyList<int>? SeriesIds() => Series is not null && TryParseSeries(Series, out var ids) ? ids : null;

    // Stored timestamps are truncated to microseconds, so the bounds are truncated the same way: truncation is
    // monotonic, so a value inside [from, to] stays inside after both are truncated. (Rounding "from" up instead
    // would drop a result posted at exactly "…00.1234567" and stored as "…00.123456".)
    public DateTimeOffset? FromValue() =>
        IsoDateTime.TryParseQueryValue(From, out var value) ? value.TruncateToMicroseconds() : null;

    public DateTimeOffset? ToValue() =>
        IsoDateTime.TryParseQueryValue(To, out var value) ? value.TruncateToMicroseconds() : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Series is not null && !TryParseSeries(Series, out _))
            yield return new ValidationResult("Oczekiwano identyfikatorów serii rozdzielonych przecinkami, np. 1,3.", ["series"]);
        if (From is not null && !IsoDateTime.TryParseQueryValue(From, out _))
            yield return new ValidationResult(IsoDateTimeAttribute.Message, ["from"]);
        if (To is not null && !IsoDateTime.TryParseQueryValue(To, out _))
            yield return new ValidationResult(IsoDateTimeAttribute.Message, ["to"]);
    }

    private static bool TryParseSeries(string? text, out IReadOnlyList<int> ids)
    {
        var parsed = new List<int>();
        ids = parsed;
        if (text is null)
            return true;

        foreach (var part in text.Split(','))
        {
            if (!int.TryParse(part.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
                return false;
            parsed.Add(id);
        }

        return true;
    }
}
