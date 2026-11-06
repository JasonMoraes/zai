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
            // The database enforces the core rules too, so no code path (now or later) can store an invalid series.
            series.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Series_Range", "\"MinValue\" < \"MaxValue\"");
                table.HasCheckConstraint("CK_Series_Color", "\"Color\" ~ '^#[0-9A-Fa-f]{6}$'");
                table.HasCheckConstraint("CK_Series_Name", NotBlank("Name"));
                // PostgreSQL orders NaN above every number, so "MinValue < MaxValue" alone would accept a NaN maximum.
                table.HasCheckConstraint("CK_Series_Finite", Finite("MinValue") + " AND " + Finite("MaxValue"));
            });
            series.Property(s => s.Name).HasMaxLength(FieldLimits.NameMaxLength);
            series.Property(s => s.Color).HasMaxLength(FieldLimits.ColorLength);
            series.Property(s => s.Unit).HasMaxLength(FieldLimits.UnitMaxLength);
        });

        modelBuilder.Entity<Sensor>(sensor =>
        {
            sensor.ToTable(table => table.HasCheckConstraint("CK_Sensors_Name", NotBlank("Name")));
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
            // PostgreSQL double precision can hold NaN and ±Infinity; a measurement never may.
            measurement.ToTable(table => table.HasCheckConstraint("CK_Measurements_Value_Finite", Finite("Value")));
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

    // Not empty or whitespace only (tabs and newlines included, unlike btrim, which only strips spaces).
    private static string NotBlank(string column) => $"\"{column}\" !~ '^[[:space:]]*$'";

    // double precision can hold NaN and ±Infinity; none of these columns ever may. (In PostgreSQL NaN = NaN is true,
    // so <> comparisons work.)
    private static string Finite(string column) =>
        $"\"{column}\" <> 'NaN'::float8 AND \"{column}\" <> 'Infinity'::float8 AND \"{column}\" <> '-Infinity'::float8";
}
