using Microsoft.EntityFrameworkCore;
using Timescale.API.Models;

namespace Timescale.API.Data;

public abstract class BaseAppDbContext(DbContextOptions options) : DbContext(options), IVehicleRepository

{
    public DbSet<VehicleTelemetry> VehicleTelemetries { get; set; }
    public DbSet<VehicleHourlyStats> VehicleHourlyStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VehicleTelemetry>(entity =>
        {
            entity.HasNoKey();

            entity.HasIndex(vt => new { vt.VehicleId, vt.Timestamp }).IsDescending(false, true);
        });

        modelBuilder.Entity<VehicleHourlyStats>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(nameof(VehicleHourlyStats));
        });
    }

    public Task<List<VehicleAlert>> GetAlertsByTypeAsync(string alertType, DateTime start, DateTime end,
        double threshold)
        => alertType.ToLowerInvariant() switch
        {
            "temperature" => this.GetHighEngineTemperatureAlertsAsync(start, end, threshold),
            "fuel" => this.GetLowFuelAlertsAsync(start, end, threshold),
            _ => throw new ArgumentException("Invalid alert type", nameof(alertType))
        };

    public Task<List<VehicleHourlyStats>> GetVehicleHourlyStatsAsync(string vehicleId, DateTime start, DateTime end)
        => SqlQueries.GetVehicleHourlyStatsAsync(this, vehicleId, start, end);

    public Task<List<VehicleUtilization>> GetVehicleUtilizationAsync(DateTime start, DateTime end)
        => SqlQueries.GetVehicleUtilizationAsync(this, start, end);

    public IAsyncEnumerable<VehicleTelemetry> GetVehicleTelemetryAsyncEnumerable(string vehicleId, DateTime start,
        DateTime end)
        => SqlQueries.GetVehicleTelemetryAsyncEnumerable(this, vehicleId, start, end);
}

public sealed class NativePostgresDbContext(DbContextOptions<NativePostgresDbContext> options)
    : BaseAppDbContext(options);

public sealed class TimescaleDbContext(DbContextOptions<TimescaleDbContext> options) : BaseAppDbContext(options);