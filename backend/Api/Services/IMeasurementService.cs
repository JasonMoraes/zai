using Api.Entities;
using Api.Services.Models;

namespace Api.Services;

public interface IMeasurementService
{
    /// <summary>Stores a result sent by a sensor; <paramref name="timestamp"/> <c>null</c> means now.</summary>
    Task<CreateMeasurementResult> CreateAsync(
        int sensorId, int seriesId, double value, DateTimeOffset? timestamp, CancellationToken cancellationToken);

    Task<IReadOnlyList<Measurement>> QueryAsync(MeasurementFilter filter, CancellationToken cancellationToken);

    Task<Measurement?> GetAsync(long id, CancellationToken cancellationToken);
}
