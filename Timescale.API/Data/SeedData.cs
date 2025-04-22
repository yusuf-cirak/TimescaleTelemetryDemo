using System.Diagnostics;
using Bogus;
using EFCore.BulkExtensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Timescale.API.Models;

namespace Timescale.API.Data;

public static class SeedData
{
    private static readonly List<string> VehicleIds =
        ["980", "457", "234", "435", "777", "10", "1", "884", "151761", "123037"];

    // Istanbul coordinates: 41.0082, 28.9784
    // Erzurum coordinates: 39.9055, 41.2658

    private static readonly (double lat, double lon) Istanbul = (41.0082, 28.9784);
    private static readonly (double lat, double lon) Erzurum = (39.9055, 41.2658);

    private static readonly List<string> EngineStatuses = ["Running", "Idle", "Off"];

    public static IEnumerable<VehicleTelemetry> Generate()
    {
        var startDate = DateTime.UtcNow.AddYears(-1);
        var increment = TimeSpan.FromSeconds(86400.0 / 1000000);

        var faker = new Faker<VehicleTelemetry>()
            .RuleFor(f => f.Timestamp,
                (f, vt) => startDate.AddMonths(Random.Shared.Next(0, 11)).Add(increment * f.IndexFaker))
            .RuleFor(f => f.VehicleId, f => f.PickRandom(VehicleIds))
            .RuleFor(f => f.Latitude, f => f.Random.Double(-90, 90))
            .RuleFor(f => f.Longitude, f => f.Random.Double(-180, 180))
            .RuleFor(f => f.Speed, f => f.Random.Bool(0.5f) ? f.Random.Double(0, 200) : null)
            .RuleFor(f => f.FuelLevel, f => f.Random.Bool(0.5f) ? f.Random.Double(0, 100) : null)
            .RuleFor(f => f.EngineTemperature, f => f.Random.Bool(0.5f) ? f.Random.Double(50, 120) : null)
            .RuleFor(f => f.EngineRpm, f => f.Random.Bool(0.5f) ? f.Random.Int(0, 8000) : null)
            .RuleFor(f => f.BatteryVoltage, f => f.Random.Bool(0.5f) ? f.Random.Double(11.0, 14.8) : null)
            .RuleFor(f => f.EngineStatus, f => f.Random.Bool(0.5f) ? f.PickRandom(EngineStatuses) : null)
            .RuleFor(f => f.OdometerReading, (f, vt) =>
            {
                // Start with a base reading between 10000-30000 for each vehicle
                var baseReading = f.Random.Double(10000, 30000);
                // Add progressive kilometers (approximately 0.1 km per record)
                return baseReading + (f.IndexFaker * 0.1);
            });


        return faker.GenerateLazy(VehicleIds.Count * 1_000_000);
    }


    public static async Task PersistAsync(this BaseAppDbContext dbContext, List<VehicleTelemetry> vehicleTelemetries)
    {
        Console.WriteLine("------------------------------------------------------");
        Console.WriteLine($"Seeding {dbContext.GetType().Name} with {vehicleTelemetries.Count} records...");
        var ts = Stopwatch.GetTimestamp();

        await dbContext.BulkInsertAsync(vehicleTelemetries);

        Console.WriteLine(
            $"Seeding completed for {dbContext.GetType().Name} in {Stopwatch.GetElapsedTime(ts).TotalSeconds} seconds for {vehicleTelemetries.Count} records.");
    }


    public static async Task PersistAsync(this ElasticsearchClient elasticsearchClient,
        List<VehicleTelemetry> vehicleTelemetries)
    {
        try
        {
            await elasticsearchClient.Transport.PutAsync<StringResponse>("vehicle-telemetry/_settings",
                PostData.Serializable(new { max_result_window = 100_000 }));

            Console.WriteLine($"Seeding Elasticsearch with {vehicleTelemetries.Count} records...");

            var ts = Stopwatch.GetTimestamp();

            var chunkSize = 100_000;

            var tasks = vehicleTelemetries
                .Chunk(chunkSize)
                .Select(chunk => elasticsearchClient.BulkAsync(b => b
                    .Index(VehicleTelemetry.IndexName)
                    .IndexMany(chunk)
                ))
                .ToList();

            await Task.WhenAll(tasks);

            await elasticsearchClient.Indices.RefreshAsync(VehicleTelemetry.IndexName);

            Console.WriteLine(
                $"Seeding completed for Elasticsearch in {Stopwatch.GetElapsedTime(ts).TotalSeconds} seconds for {vehicleTelemetries.Count} records.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}