using Api.Entities;
using Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Measurement> Measurements => Set<Measurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(user =>
        {
            user.Property(u => u.Username).HasMaxLength(FieldLimits.UsernameMaxLength);
            user.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<RevokedToken>(token =>
        {
            token.HasKey(t => t.Jti);
            token.Property(t => t.Jti).HasMaxLength(RevokedToken.JtiMaxLength);
            token.HasIndex(t => t.ExpiresAt);
        });

        modelBuilder.Entity<Series>(series =>
        {
            series.Property(s => s.Name).HasMaxLength(FieldLimits.NameMaxLength);
            series.Property(s => s.Color).HasMaxLength(FieldLimits.ColorLength);
            series.Property(s => s.Unit).HasMaxLength(FieldLimits.UnitMaxLength);
        });

        modelBuilder.Entity<Sensor>(sensor =>
        {
            sensor.Property(s => s.Name).HasMaxLength(FieldLimits.NameMaxLength);
            sensor.Property(s => s.ApiKeyHash).HasMaxLength(Sensor.ApiKeyHashLength).IsFixedLength();
            // Unique: measurement uploads look the sensor up by the hash of the key they present.
            sensor.HasIndex(s => s.ApiKeyHash).IsUnique();
            // Contract: deleting a series removes its sensors too.
            sensor.HasOne(s => s.Series).WithMany(s => s.Sensors).HasForeignKey(s => s.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Measurement>(measurement =>
        {
            // Contract: deleting a series removes its results; unregistering a sensor keeps them.
            measurement.HasOne(m => m.Series).WithMany().HasForeignKey(m => m.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
            measurement.HasOne(m => m.Sensor).WithMany().HasForeignKey(m => m.SensorId)
                .OnDelete(DeleteBehavior.SetNull);
            // Serves both the per-series time-range query and its ordering; the second covers unfiltered queries.
            measurement.HasIndex(m => new { m.SeriesId, m.Timestamp });
            measurement.HasIndex(m => m.Timestamp);
            // Lets a series PUT check "any saved result outside the new range" without scanning the whole series
            // while it holds the series lock.
            measurement.HasIndex(m => new { m.SeriesId, m.Value });
        });
    }
}
