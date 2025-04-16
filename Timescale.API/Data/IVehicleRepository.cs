using Timescale.API.Models;

namespace Timescale.API.Data;

public interface IVehicleRepository
{
    Task<List<VehicleAlert>> GetAlertsByTypeAsync(string alertType, DateTime start, DateTime end, double threshold);
    Task<List<VehicleHourlyStats>> GetVehicleHourlyStatsAsync(string vehicleId, DateTime start, DateTime end);
    Task<List<VehicleUtilization>> GetVehicleUtilizationAsync(DateTime start, DateTime end);
    IAsyncEnumerable<VehicleTelemetry> GetVehicleTelemetryAsyncEnumerable(string vehicleId, DateTime start, DateTime end);
}