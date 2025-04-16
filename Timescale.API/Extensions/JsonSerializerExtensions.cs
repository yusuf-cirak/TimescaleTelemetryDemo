using System.Text.Json.Serialization;
using Timescale.API.Data;
using Timescale.API.Models;

namespace Timescale.API.Extensions;

public static class JsonSerializerExtensions
{
    public static void ConfigureSourceGeneratedJsonSerializer
        (this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureHttpJsonOptions(opt =>
        {
            opt.SerializerOptions.TypeInfoResolver = TimescalePostgresAPIJsonSerializerContext.Default;
        });
    }
}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(VehicleTelemetry))]
[JsonSerializable(typeof(IAsyncEnumerable<VehicleTelemetry>))]
[JsonSerializable(typeof(VehicleHourlyStats))]
[JsonSerializable(typeof(List<VehicleHourlyStats>))]
[JsonSerializable(typeof(IOrderedEnumerable<VehicleHourlyStats>))]
[JsonSerializable(typeof(VehicleAlert))]
[JsonSerializable(typeof(List<VehicleAlert>))]
[JsonSerializable(typeof(IOrderedEnumerable<VehicleAlert>))]
[JsonSerializable(typeof(VehicleUtilization))]
[JsonSerializable(typeof(List<VehicleUtilization>))]
[JsonSerializable(typeof(IOrderedEnumerable<VehicleUtilization>))]
[JsonSerializable(typeof(DatabaseType))]
public partial class TimescalePostgresAPIJsonSerializerContext : JsonSerializerContext;