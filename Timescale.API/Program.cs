using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Timescale.API.Data;
using Timescale.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddDbContext<NativePostgresDbContext>(opt =>
{
    opt.UseNpgsql("Host=localhost;Database=Shared_Native;Username=postgres;Password=password");
    opt.LogTo(
        (eventData) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Native PostgreSQL] {eventData}");
            Console.ResetColor();
        },
        LogLevel.Information
    );
    opt.EnableDetailedErrors();
});

builder.Services.AddDbContext<TimescaleDbContext>(opt =>
{
    opt.UseNpgsql("Host=localhost;Database=Shared_Timescale;Username=postgres;Password=password");
    opt.LogTo(
        (eventData) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[TimescaleDB] {eventData}");
            Console.ResetColor();
        },
        LogLevel.Information
    );
    opt.EnableDetailedErrors();
});


builder.Services.AddSingleton<ElasticsearchClient>(b =>
    new(new ElasticsearchClientSettings(new Uri("http://localhost:9200"))
        .DisableDirectStreaming(false)
        .EnableHttpCompression()
        .EnableTcpKeepAlive(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))));


builder.Services.AddSingleton<ElasticRepository>();

builder.Services.AddOpenApi();

builder.ConfigureSourceGeneratedJsonSerializer();

builder.AddCompression();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    await app.SeedAsync();
}

app.UseCompression();

app.MapVehicleTelemetryEndpoints();

app.Run();