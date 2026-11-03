namespace Api.Services;

/// <summary>The editable fields of a series, as given on create or full update.</summary>
public record SeriesValues(string Name, double MinValue, double MaxValue, string Color, string? Icon, string? Unit);
