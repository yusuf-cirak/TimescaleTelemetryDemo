using Microsoft.EntityFrameworkCore;
using Timescale.API.Models;

namespace Timescale.API.Data;

public static partial class SqlQueries
{
    public static IAsyncEnumerable<VehicleTelemetry> GetVehicleTelemetryAsyncEnumerable(this BaseAppDbContext dbContext,
        string vehicleId, DateTime start, DateTime end)
        => dbContext
            .VehicleTelemetries
            .AsNoTracking()
            .Where(t => t.VehicleId == vehicleId && t.Timestamp >= start && t.Timestamp <= end)
            .AsAsyncEnumerable();

    // Get hourly stats for a specific vehicle
    public static Task<List<VehicleHourlyStats>> GetVehicleHourlyStatsAsync(this BaseAppDbContext dbContext,
        string vehicleId, DateTime start, DateTime end)
        => dbContext
            .VehicleHourlyStats
            .AsNoTracking()
            .Where(s => s.VehicleId == vehicleId &&
                        s.TimeWindow >= start &&
                        s.TimeWindow <= end)
            .OrderByDescending(s => s.TimeWindow)
            .ToListAsync();

    // Get vehicles with high engine temperature alerts
    public static Task<List<VehicleAlert>> GetHighEngineTemperatureAlertsAsync(this BaseAppDbContext dbContext,
        DateTime start, DateTime end, double temperatureThreshold = 100.0)
        => dbContext
            .VehicleHourlyStats
            .AsNoTracking()
            .Where(s => s.TimeWindow >= start &&
                        s.TimeWindow <= end &&
                        s.MaxEngineTemp > temperatureThreshold)
            .OrderByDescending(s => s.MaxEngineTemp)
            .Select(s => new VehicleAlert
            {
                VehicleId = s.VehicleId,
                Timestamp = s.TimeWindow,
                Value = s.MaxEngineTemp ?? 0,
                AlertType = "HighEngineTemperature"
            })
            .OrderBy(d => d.VehicleId)
            .ToListAsync();

    // Get vehicles with low fuel level alerts
    public static Task<List<VehicleAlert>> GetLowFuelAlertsAsync(this BaseAppDbContext dbContext,
        DateTime start, DateTime end, double fuelThreshold = 15.0)
        => dbContext
            .VehicleHourlyStats
            .AsNoTracking()
            .Where(s => s.TimeWindow >= start &&
                        s.TimeWindow <= end &&
                        s.MinFuelLevel < fuelThreshold)
            .OrderBy(s => s.MinFuelLevel)
            .Select(s => new VehicleAlert
            {
                VehicleId = s.VehicleId,
                Timestamp = s.TimeWindow,
                Value = s.MinFuelLevel ?? 0,
                AlertType = "LowFuel"
            })
            .OrderBy(d => d.VehicleId)
            .ToListAsync();

    // Get vehicle utilization report
    public static Task<List<VehicleUtilization>> GetVehicleUtilizationAsync(this BaseAppDbContext dbContext,
        DateTime start, DateTime end)
        => dbContext
            .VehicleHourlyStats
            .AsNoTracking()
            .Where(s => s.TimeWindow >= start && s.TimeWindow <= end)
            .GroupBy(s => s.VehicleId)
            .Select(g => new VehicleUtilization
            {
                VehicleId = g.Key,
                TotalHours = g.Count(),
                RunningHours = g.Count(s => s.MostCommonEngineStatus == "Running"),
                IdleHours = g.Count(s => s.MostCommonEngineStatus == "Idle"),
                UtilizationRate = (double)g.Count(s => s.MostCommonEngineStatus == "Running") / g.Count() * 100,
                AverageMovingSpeed = g.Where(s => s.AvgSpeed > 0).Average(s => s.AvgSpeed) ?? 0,
                DistanceCovered = g.Max(s => s.LastOdometerReading) - g.Min(s => s.LastOdometerReading)
            })
            .OrderBy(v => v.VehicleId)
            .ToListAsync();
}

public class VehicleAlert
{
    public required string VehicleId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public required string AlertType { get; set; }
}

public class VehicleUtilization
{
    public required string VehicleId { get; set; }
    public int TotalHours { get; set; }
    public int RunningHours { get; set; }
    public int IdleHours { get; set; }
    public double UtilizationRate { get; set; }
    public double AverageMovingSpeed { get; set; }
    public double DistanceCovered { get; set; }
}