using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Api.Validation;

/// <summary>
/// Strict ISO 8601 date-time parsing: the zone (<c>Z</c> or <c>±hh:mm</c>) is mandatory, as the contract requires.
/// Lenient parsers would read a zone-less value as server-local time, silently shifting it.
/// </summary>
public static partial class IsoDateTime
{
    /// <summary>Parses and normalises to UTC (PostgreSQL <c>timestamptz</c> parameters must have offset 0).</summary>
    public static bool TryParse(string? text, out DateTimeOffset value)
    {
        value = default;
        if (text is null || !Pattern().IsMatch(text)
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;

        value = parsed.ToUniversalTime();
        return true;
    }

    /// <summary>
    /// For query strings: an unencoded "+" in "…T08:30:00+02:00" arrives as a space, so a space in the
    /// offset position is read as "+".
    /// </summary>
    public static bool TryParseQueryValue(string? text, out DateTimeOffset value) =>
        TryParse(text is null ? null : SpaceForPlus().Replace(text, "+$1"), out value);

    // yyyy-MM-ddTHH:mm[:ss[.fffffff]] followed by Z or ±hh:mm. ASCII digits only ([0-9], not \d, which matches any
    // Unicode digit) and \z, not $, which would also accept a trailing newline.
    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}[Tt][0-9]{2}:[0-9]{2}(:[0-9]{2}(\.[0-9]{1,7})?)?([Zz]|[+-][0-9]{2}:[0-9]{2})\z")]
    private static partial Regex Pattern();

    [GeneratedRegex(@" ([0-9]{2}:[0-9]{2})\z")]
    private static partial Regex SpaceForPlus();
}

/// <summary>Validates a string as an ISO 8601 date-time with a zone (see <see cref="IsoDateTime"/>).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class IsoDateTimeAttribute() : ValidationAttribute(Message)
{
    public const string Message = "Oczekiwano daty i czasu w formacie ISO 8601 ze strefą czasową, np. 2026-10-15T08:30:00Z.";

    public override bool IsValid(object? value) => value is not string text || IsoDateTime.TryParse(text, out _);
}
