using Api.Entities;

namespace Api.Services;

public interface ISensorService
{
    Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken cancellationToken);
    Task<Sensor?> GetAsync(int id, CancellationToken cancellationToken);

    /// <summary>Returns <c>null</c> when the series does not exist.</summary>
    Task<SensorRegistration?> RegisterAsync(string name, int seriesId, CancellationToken cancellationToken);

    /// <summary>Returns <c>false</c> when the sensor does not exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
