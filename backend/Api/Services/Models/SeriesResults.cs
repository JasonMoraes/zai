using Api.Entities;

namespace Api.Services.Models;

public enum SeriesWriteStatus
{
    Done,
    /// <summary>minValue ≥ maxValue (422).</summary>
    InvalidRange,
    NotFound,
    /// <summary>The new range would exclude results already saved in the series (409).</summary>
    ExcludesSavedMeasurements,
}

public record SeriesWriteResult(SeriesWriteStatus Status, Series? Series = null);
