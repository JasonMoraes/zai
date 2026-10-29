using Api.Data;
using Api.Entities;
using Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public class SensorService(
    AppDbContext db,
    IApiKeyGenerator apiKeys,
    TimeProvider time,
    ILogger<SensorService> logger) : ISensorService
{
    public async Task<IReadOnlyList<Sensor>> ListAsync(CancellationToken cancellationToken) =>
        await db.Sensors.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken);

    public Task<Sensor?> GetAsync(int id, CancellationToken cancellationToken) =>
        db.Sensors.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<SensorRegistration?> RegisterAsync(string name, int seriesId, CancellationToken cancellationToken)
    {
        if (!await db.Series.AnyAsync(s => s.Id == seriesId, cancellationToken))
            return null;

        var key = apiKeys.Generate();
        var sensor = new Sensor
        {
            Name = name,
            SeriesId = seriesId,
            ApiKeyHash = key.Hash,
            CreatedAt = time.GetUtcNow().TruncateToMicroseconds(),
        };
        db.Sensors.Add(sensor);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // The series was deleted between the check above and the insert.
            return null;
        }

        logger.LogInformation("Sensor {SensorId} registered for series {SeriesId}", sensor.Id, seriesId);
        return new SensorRegistration(sensor, key.Key);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await db.Sensors.Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
        if (deleted)
            logger.LogInformation("Sensor {SensorId} unregistered", id);

        return deleted;
    }
}
