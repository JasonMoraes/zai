using Api.Data;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class SeriesService(AppDbContext db, ILogger<SeriesService> logger) : ISeriesService
{
    public async Task<IReadOnlyList<Series>> ListAsync(CancellationToken cancellationToken) =>
        await db.Series.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken);

    public Task<Series?> GetAsync(int id, CancellationToken cancellationToken) =>
        db.Series.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<SeriesWriteResult> CreateAsync(SeriesValues values, CancellationToken cancellationToken)
    {
        if (!HasValidRange(values))
            return new SeriesWriteResult(SeriesWriteStatus.InvalidRange);

        var series = new Series
        {
            Name = values.Name,
            MinValue = values.MinValue,
            MaxValue = values.MaxValue,
            Color = values.Color,
            Icon = values.Icon,
            Unit = values.Unit,
        };
        db.Series.Add(series);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Series {SeriesId} created", series.Id);
        return new SeriesWriteResult(SeriesWriteStatus.Done, series);
    }

    public async Task<SeriesWriteResult> UpdateAsync(int id, SeriesValues values, CancellationToken cancellationToken)
    {
        if (!HasValidRange(values))
            return new SeriesWriteResult(SeriesWriteStatus.InvalidRange);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // The UPDATE takes the series row lock, which uploads also take before checking a value against the range
        // (see MeasurementService). So no upload validated against the old range can commit while this checks.
        var updated = await db.Series.Where(s => s.Id == id).ExecuteUpdateAsync(set => set
            .SetProperty(s => s.Name, values.Name)
            .SetProperty(s => s.MinValue, values.MinValue)
            .SetProperty(s => s.MaxValue, values.MaxValue)
            .SetProperty(s => s.Color, values.Color)
            .SetProperty(s => s.Icon, values.Icon)
            .SetProperty(s => s.Unit, values.Unit), cancellationToken);
        if (updated == 0)
            return new SeriesWriteResult(SeriesWriteStatus.NotFound);

        var excludesSaved = await db.Measurements.AnyAsync(
            m => m.SeriesId == id && (m.Value < values.MinValue || m.Value > values.MaxValue), cancellationToken);
        if (excludesSaved)
            return new SeriesWriteResult(SeriesWriteStatus.ExcludesSavedMeasurements); // disposing rolls back

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Series {SeriesId} updated", id);

        // Return what this request wrote rather than re-reading: once committed the lock is gone, and a concurrent
        // DELETE or PUT could make a re-read fail (500 for a PUT that succeeded) or return someone else's values.
        var series = new Series
        {
            Id = id,
            Name = values.Name,
            MinValue = values.MinValue,
            MaxValue = values.MaxValue,
            Color = values.Color,
            Icon = values.Icon,
            Unit = values.Unit,
        };
        return new SeriesWriteResult(SeriesWriteStatus.Done, series);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        // Results and sensors go with it through the foreign keys' ON DELETE CASCADE.
        var deleted = await db.Series.Where(s => s.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
        if (deleted)
            logger.LogInformation("Series {SeriesId} deleted with its results and sensors", id);

        return deleted;
    }

    private static bool HasValidRange(SeriesValues values) => values.MinValue < values.MaxValue;
}
