using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Timescale.API.Data;
using Timescale.API.Models;

namespace Timescale.API.Extensions;

public static class VehicleTelemetryEndpointExtensions
{
    public static void MapVehicleTelemetryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/telemetry", (
            [FromQuery] string vehicleId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] DatabaseType dbType,
            IServiceProvider serviceProvider) =>
        {
            IVehicleRepository repository = GetRepository(dbType, serviceProvider);
            return repository.GetVehicleTelemetryAsyncEnumerable(vehicleId, start, end);
        });


        var group = endpoints.MapGroup("/api/v1/analytics");


        group.MapGet("stats", async (
            [FromQuery] string vehicleId,
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] DatabaseType dbType,
            IServiceProvider serviceProvider) =>
        {
            IVehicleRepository repository = GetRepository(dbType, serviceProvider);
            return (await repository.GetVehicleHourlyStatsAsync(vehicleId, start, end)).OrderBy(s=>s.VehicleId);
        });

        group.MapGet("alerts", async (
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] double threshold,
            [FromQuery] string alertType,
            [FromQuery] DatabaseType dbType,
            IServiceProvider serviceProvider) =>
        {
            IVehicleRepository repository = GetRepository(dbType, serviceProvider);

            return await repository.GetAlertsByTypeAsync(alertType, start, end, threshold);
        });

        group.MapGet("utilization", async (
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            [FromQuery] DatabaseType dbType,
            IServiceProvider serviceProvider) =>
        {
            IVehicleRepository repository = GetRepository(dbType, serviceProvider);
            return await repository.GetVehicleUtilizationAsync(start, end);
        });
    }

    private static IVehicleRepository GetRepository(DatabaseType dbType, IServiceProvider serviceProvider) =>
        dbType switch
        {
            DatabaseType.Native => GetDbContext<NativePostgresDbContext>(serviceProvider),
            DatabaseType.Timescale => GetDbContext<TimescaleDbContext>(serviceProvider),
            DatabaseType.Elastic => new ElasticRepository(serviceProvider.GetRequiredService<ElasticsearchClient>()),
            _ => throw new ArgumentException("Invalid database type", nameof(dbType))
        };

    private static T GetDbContext<T>(IServiceProvider serviceProvider) where T : BaseAppDbContext
    {
        return serviceProvider.GetRequiredService<T>();
    }
}