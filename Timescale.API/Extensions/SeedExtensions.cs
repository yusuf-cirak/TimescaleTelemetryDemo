using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Timescale.API.Data;
using Timescale.API.Models;

namespace Timescale.API.Extensions;

public static class SeedExtensions
{
    public static async Task SeedAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        List<BaseAppDbContext> dbContexts =
        [
            scope.ServiceProvider.GetRequiredService<NativePostgresDbContext>(),
            scope.ServiceProvider.GetRequiredService<TimescaleDbContext>(),
        ];
        
        var elasticService = scope.ServiceProvider.GetRequiredService<ElasticsearchClient>();

        // do migrations manually
        // foreach (var dbContext in dbContexts)
        // {
        //     await dbContext.Database.EnsureCreatedAsync();
        //     await dbContext.Database.MigrateAsync();
        // }

        
        await elasticService.Transport.PutAsync<StringResponse>("vehicle-telemetry/_settings", 
            PostData.Serializable(new { max_result_window = 100_000 }));

        if ((await dbContexts.First().VehicleTelemetries.AnyAsync()))
        {
            return;
        }

        var data = SeedData.Generate().ToList();

        var tasks = dbContexts.Select(dbContext => dbContext.PersistAsync(data)).ToList();
        
        tasks.Add(elasticService.PersistAsync(data));

        await Task.WhenAll(tasks);
    }
}