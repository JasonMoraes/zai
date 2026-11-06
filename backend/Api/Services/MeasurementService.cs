using Api.Data;
using Api.Entities;
using Api.Extensions;
using Api.Services.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Services;

public class MeasurementService(AppDbContext db, TimeProvider time, ILogger<MeasurementService> logger)
    : IMeasurementService
{
    public async Task<CreateMeasurementResult> CreateAsync(
        int sensorId, int seriesId, double value, DateTimeOffset? timestamp, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Take the series row lock before reading its range. A PUT narrowing the range takes the same lock before it
        // checks saved results, so a value validated against the old range can't slip in while it checks (and the
        // range read below is the latest committed one). EF Core has no SELECT … FOR UPDATE, and raw SQL is ruled
        // out, so a no-op UPDATE takes the lock. Cost: uploads to the same series run one at a time.
        var locked = await db.Series.Where(s => s.Id == seriesId)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.Name, s => s.Name), cancellationToken);
        if (locked == 0)
            return CreateMeasurementResult.Gone(); // deleting a series cascades to its sensors

        var series = await db.Series.AsNoTracking().SingleAsync(s => s.Id == seriesId, cancellationToken);
        var now = time.GetUtcNow();
        var at = (timestamp ?? now).ToUniversalTime().TruncateToMicroseconds();

        var violations = new List<MeasurementRuleViolation>();
        // Written as "not inside" so a NaN (which compares false with everything) can never pass.
        if (!(value >= series.MinValue && value <= series.MaxValue))
            violations.Add(MeasurementRuleViolation.ValueOutOfRange);
        if (at > now + MeasurementRules.MaxFutureSkew)
            violations.Add(MeasurementRuleViolation.TimestampTooFarInFuture);
        if (violations.Count > 0)
        {
            // F4: every rejected result is recorded in the error log.
            logger.LogWarning(
                "Rejected measurement from sensor {SensorId} for series {SeriesId}: value {Value} at {Timestamp} broke {Violations} (allowed range [{MinValue}, {MaxValue}])",
                sensorId, seriesId, value, at, string.Join(", ", violations), series.MinValue, series.MaxValue);
            return new CreateMeasurementResult(null, series, violations); // disposing rolls back, releasing the lock
        }

        var measurement = new Measurement { SeriesId = seriesId, SensorId = sensorId, Value = value, Timestamp = at };
        db.Measurements.Add(measurement);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // The sensor was deleted after its key was checked (the series can't be: we hold its lock).
            return CreateMeasurementResult.Gone();
        }

        // Newest measurement timestamp from this sensor; a single conditional UPDATE, so concurrent uploads
        // and back-filled historical data can never move it backwards.
        await db.Sensors
            .Where(s => s.Id == sensorId && (s.LastMeasurementAt == null || s.LastMeasurementAt < at))
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.LastMeasurementAt, at), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new CreateMeasurementResult(measurement, series, []);
    }

    public async Task<IReadOnlyList<Measurement>> QueryAsync(MeasurementFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Measurements.AsNoTracking();

        if (filter.SeriesIds is { } seriesIds)
            query = query.Where(m => seriesIds.Contains(m.SeriesId));
        if (filter.From is { } from)
            query = query.Where(m => m.Timestamp >= from);
        if (filter.To is { } to)
            query = query.Where(m => m.Timestamp <= to);

        // Id breaks ties so results with equal timestamps come back in a stable order.
        query = filter.Descending
            ? query.OrderByDescending(m => m.Timestamp).ThenByDescending(m => m.Id)
            : query.OrderBy(m => m.Timestamp).ThenBy(m => m.Id);

        return await query.Take(filter.Limit).ToListAsync(cancellationToken);
    }

    public Task<Measurement?> GetAsync(long id, CancellationToken cancellationToken) =>
        db.Measurements.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
}
