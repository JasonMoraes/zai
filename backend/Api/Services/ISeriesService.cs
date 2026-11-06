using Api.Entities;
using Api.Services.Models;

namespace Api.Services;

public interface ISeriesService
{
    Task<IReadOnlyList<Series>> ListAsync(CancellationToken cancellationToken);
    Task<Series?> GetAsync(int id, CancellationToken cancellationToken);
    Task<SeriesWriteResult> CreateAsync(SeriesValues values, CancellationToken cancellationToken);

    /// <summary>Replaces every editable field; refuses a range that would exclude saved results.</summary>
    Task<SeriesWriteResult> UpdateAsync(int id, SeriesValues values, CancellationToken cancellationToken);

    /// <summary>Deletes the series with its results and sensors; <c>false</c> when it does not exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
