using Elastic.Clients.Elasticsearch;
using Timescale.API.Models;

namespace Timescale.API.Data;

public class ElasticRepository(ElasticsearchClient client) : IVehicleRepository
{
    public Task<List<VehicleAlert>> GetHighEngineTemperatureAlertsAsync(DateTime start, DateTime end, double threshold)
        => client.GetHighEngineTemperatureAlertsAsync(start, end, threshold);

    public Task<List<VehicleAlert>> GetLowFuelAlertsAsync(DateTime start, DateTime end, double threshold)
        => client.GetLowFuelAlertsAsync(start, end, threshold);

    public Task<List<VehicleAlert>> GetAlertsByTypeAsync(string alertType, DateTime start, DateTime end,
        double threshold)
        => alertType.ToLowerInvariant() switch
        {
            "temperature" => GetHighEngineTemperatureAlertsAsync(start, end, threshold),
            "fuel" => GetLowFuelAlertsAsync(start, end, threshold),
            _ => throw new ArgumentException("Invalid alert type", nameof(alertType))
        };

    public Task<List<VehicleHourlyStats>> GetVehicleHourlyStatsAsync(string vehicleId, DateTime start, DateTime end)
        => client.GetVehicleHourlyStatsAsync(vehicleId, start, end);

    public Task<List<VehicleUtilization>> GetVehicleUtilizationAsync(DateTime start, DateTime end)
        => client.GetVehicleUtilizationAsync(start, end);

    public IAsyncEnumerable<VehicleTelemetry> GetVehicleTelemetryAsyncEnumerable(string vehicleId, DateTime start,
        DateTime end)
        => client.GetVehicleTelemetryAsyncEnumerable(vehicleId, start, end);
}