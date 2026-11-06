namespace Api.Services.Models;

/// <param name="SeriesIds"><c>null</c> means all series.</param>
/// <param name="From">Inclusive lower bound.</param>
/// <param name="To">Inclusive upper bound.</param>
public record MeasurementFilter(
    IReadOnlyCollection<int>? SeriesIds, DateTimeOffset? From, DateTimeOffset? To, bool Descending, int Limit);
