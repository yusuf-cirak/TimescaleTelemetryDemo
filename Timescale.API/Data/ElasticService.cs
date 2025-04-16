using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Timescale.API.Models;

namespace Timescale.API.Data;

public static class ElasticQueries
{
    public static async Task<List<VehicleHourlyStats>> GetVehicleHourlyStatsAsync(
        this ElasticsearchClient client,
        string vehicleId,
        DateTime start,
        DateTime end)
    {
        var response = await client.SearchAsync<VehicleTelemetry>(s => s
            .Index(VehicleTelemetry.IndexName)
            .Size(0)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field(f => f.VehicleId.Suffix("keyword")).Value(vehicleId)),
                        m => m.Range(r => r
                            .DateRange(dr => dr.Field(f => f.Timestamp).Gte(start).Lte(end))
                        )
                    )
                )
            )
            .Aggregations(a => a
                .Add("hourly", s => s.DateHistogram(dh => dh
                    .Field(f => f.Timestamp)
                    .FixedInterval(new Duration(TimeSpan.FromHours(1)))
                ).Aggregations(aa => aa
                    .Add("avgSpeed", s => s.Avg(avg => avg.Field(f => f.Speed)))
                    .Add("maxSpeed", s => s.Max(max => max.Field(f => f.Speed)))
                    .Add("minSpeed", s => s.Min(min => min.Field(f => f.Speed)))
                    .Add("avgFuelLevel", s => s.Avg(avg => avg.Field(f => f.FuelLevel)))
                    .Add("minFuelLevel", s => s.Min(min => min.Field(f => f.FuelLevel)))
                    .Add("avgEngineTemp", s => s.Avg(avg => avg.Field(f => f.EngineTemperature)))
                    .Add("maxEngineTemp", s => s.Max(max => max.Field(f => f.EngineTemperature)))
                    .Add("avgEngineRpm", s => s.Avg(avg => avg.Field(f => f.EngineRpm)))
                    .Add("avgBatteryVoltage", s => s.Avg(avg => avg.Field(f => f.BatteryVoltage)))
                    .Add("lastOdometer", s => s.Max(max => max.Field(f => f.OdometerReading)))
                    .Add("engineStatus", s => s.Terms(t => t.Field(f => f.EngineStatus.Suffix("keyword")).Size(1)))
                ))
            ));

        var buckets = response.Aggregations.GetDateHistogram("hourly").Buckets.ToList();
        return buckets.Select(bucket => new VehicleHourlyStats
            {
                VehicleId = vehicleId,
                TimeWindow = DateTimeOffset.FromUnixTimeMilliseconds((long)bucket.Key).UtcDateTime,
                AvgSpeed = bucket.Aggregations.GetAverage("avgSpeed")?.Value,
                MaxSpeed = bucket.Aggregations.GetMax("maxSpeed").Value,
                MinSpeed = bucket.Aggregations.GetMin("minSpeed").Value,
                AvgFuelLevel = bucket.Aggregations.GetAverage("avgFuelLevel").Value,
                MinFuelLevel = bucket.Aggregations.GetMin("minFuelLevel").Value,
                AvgEngineTemp = bucket.Aggregations.GetAverage("avgEngineTemp").Value,
                MaxEngineTemp = bucket.Aggregations.GetMax("maxEngineTemp").Value,
                AvgEngineRpm = bucket.Aggregations.GetAverage("avgEngineRpm").Value,
                AvgBatteryVoltage = bucket.Aggregations.GetAverage("avgBatteryVoltage").Value,
                LastOdometerReading = bucket.Aggregations.GetMax("lastOdometer").Value ?? 0,
                ReadingCount = (int)bucket.DocCount,
                MostCommonEngineStatus =
                    bucket.Aggregations.GetStringTerms("engineStatus").Buckets.First().Key.ToString()
            })
            .OrderBy(d => d.VehicleId)
            .ToList();
    }

    public static async Task<List<VehicleAlert>> GetHighEngineTemperatureAlertsAsync(
        this ElasticsearchClient client,
        DateTime start,
        DateTime end,
        double temperatureThreshold)
    {
        var response = await client.SearchAsync<VehicleTelemetry>(s => s
            .Index(VehicleTelemetry.IndexName)
            .Size(1000)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Range(r => r
                            .DateRange(dr => dr.Field(f => f.Timestamp).Gte(start).Lte(end))
                        ),
                        m => m.Range(r => r
                            .NumberRange(nr => nr.Field(f => f.EngineTemperature).Gt(temperatureThreshold))
                        )
                    )
                )
            )
            .Sort(s => s
                .Field(f => f.EngineTemperature, new FieldSort { Order = SortOrder.Desc })
            ));

        return response.Documents
            .Select(doc => new VehicleAlert
            {
                VehicleId = doc.VehicleId,
                Timestamp = doc.Timestamp,
                Value = doc.EngineTemperature ?? 0,
                AlertType = "HighEngineTemperature"
            })
            .OrderBy(d => d.VehicleId)
            .ToList();
    }

    public static async Task<List<VehicleAlert>> GetLowFuelAlertsAsync(
        this ElasticsearchClient client,
        DateTime start,
        DateTime end,
        double fuelThreshold)
    {
        var response = await client.SearchAsync<VehicleTelemetry>(s => s
            .Index(VehicleTelemetry.IndexName)
            .Size(1000)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        m => m.Range(r => r
                            .DateRange(dr => dr.Field(f => f.Timestamp).Gte(start).Lte(end))
                        ),
                        m => m.Range(r => r
                            .NumberRange(nr => nr.Field(f => f.FuelLevel).Lt(fuelThreshold))
                        )
                    )
                )
            )
            .Sort(s => s
                .Field(f => f.FuelLevel, new FieldSort { Order = SortOrder.Asc })
            ));

        return response.Documents
            .Select(doc => new VehicleAlert
            {
                VehicleId = doc.VehicleId,
                Timestamp = doc.Timestamp,
                Value = doc.FuelLevel ?? 0,
                AlertType = "LowFuel"
            })
            .OrderBy(d => d.VehicleId)
            .ToList();
    }

    public static async Task<List<VehicleUtilization>> GetVehicleUtilizationAsync(
        this ElasticsearchClient client,
        DateTime start,
        DateTime end)
    {
        var response = await client.SearchAsync<VehicleTelemetry>(s => s
            .Index(VehicleTelemetry.IndexName)
            .Size(0)
            .Query(q => q
                .Range(r => r
                    .DateRange(dr => dr.Field(f => f.Timestamp).Gte(start).Lte(end))
                )
            )
            .Aggregations(a => a
                .Add("byVehicle", s => s.Terms(t => t
                    .Field(f => f.VehicleId.Suffix("keyword"))
                    .Size(100000)
                ).Aggregations(aa => aa
                    .Add("engineStatus", s => s.Terms(t => t
                        .Field(f => f.EngineStatus.Suffix("keyword"))
                        .Size(10)
                    ))
                    .Add("avgSpeed", s => s.Avg(avg => avg
                        .Field(f => f.Speed)
                    ))
                    .Add("odometer", s => s.Stats(st => st
                        .Field(f => f.OdometerReading)
                    ))
                )))
        );

        var results = new List<VehicleUtilization>();
        var vehicleBuckets = response.Aggregations.GetStringTerms("byVehicle").Buckets.ToList();

        foreach (var vehicleBucket in vehicleBuckets)
        {
            var engineStatusBuckets = vehicleBucket.Aggregations.GetStringTerms("engineStatus").Buckets.ToList();
            var runningCount = engineStatusBuckets
                .FirstOrDefault(b => b.Key.ToString() == "Running")?.DocCount ?? 0;
            var idleCount = engineStatusBuckets
                .FirstOrDefault(b => b.Key.ToString() == "Idle")?.DocCount ?? 0;
            var totalCount = vehicleBucket.DocCount;
            var odometerStats = vehicleBucket.Aggregations.GetStats("odometer");

            results.Add(new VehicleUtilization
            {
                VehicleId = vehicleBucket.Key.ToString(),
                TotalHours = (int)(totalCount / 60), // Assuming 1 reading per minute
                RunningHours = (int)(runningCount / 60),
                IdleHours = (int)(idleCount / 60),
                UtilizationRate = (double)runningCount / totalCount * 100,
                AverageMovingSpeed = vehicleBucket.Aggregations.GetAverage("avgSpeed").Value ?? 0,
                DistanceCovered = (odometerStats?.Max - odometerStats?.Min) ?? 0
            });
        }

        return results.OrderBy(v => v.VehicleId).ToList();
    }

    public static IAsyncEnumerable<VehicleTelemetry> GetVehicleTelemetryAsyncEnumerable(
        this ElasticsearchClient client,
        string vehicleId, DateTime start,
        DateTime end)
    {
        async IAsyncEnumerable<VehicleTelemetry> StreamResultsWithPit()
        {
            const int batchSize = 100_000;

            using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:9200") };
            var pitResponse = await httpClient.PostAsync("vehicle-telemetry/_pit?keep_alive=5m", null);

            if (!pitResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to create PIT: {await pitResponse.Content.ReadAsStringAsync()}");
            }

            var json = await pitResponse.Content.ReadAsStringAsync();
            using var pitDocument = JsonDocument.Parse(json);
            string pitId = pitDocument.RootElement.GetProperty("id").GetString()!;

            try
            {
                FieldValue[]? searchAfter = null;
                var hasMore = true;

                while (hasMore)
                {
                    var searchDescriptor = new SearchRequestDescriptor<VehicleTelemetry>()
                        .Size(batchSize)
                        .Pit(p => p.Id(pitId).KeepAlive("5m"))
                        .Index(VehicleTelemetry.IndexName)
                        .Query(q => q
                            .Bool(b => b
                                .Must(
                                    m => m.Term(t => t.Field(f => f.VehicleId.Suffix("keyword")).Value(vehicleId)),
                                    m => m.Range(r => r
                                        .DateRange(dr => dr.Field(f => f.Timestamp).Gte(start).Lte(end))
                                    )
                                )
                            )
                        )
                        .Sort(s => s
                            .Field(f => f.Timestamp, new FieldSort { Order = SortOrder.Asc })
                        );

                    if (searchAfter != null)
                    {
                        searchDescriptor = searchDescriptor.SearchAfter(searchAfter);
                    }

                    var response = await client.SearchAsync(searchDescriptor);

                    if (!response.Documents.Any())
                    {
                        hasMore = false;
                        continue;
                    }

                    foreach (var doc in response.Documents)
                    {
                        yield return doc;
                    }

                    var lastHit = response.Hits.Last();
                    searchAfter = lastHit.Sort?.ToArray();
                }
            }
            finally
            {
                await client.ClosePointInTimeAsync(
                    new ClosePointInTimeRequestDescriptor().Id(pitId));
            }
        }

        return StreamResultsWithPit();
    }
}