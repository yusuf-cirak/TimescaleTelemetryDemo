using System.Text.Json;
using System.Text.Json.Serialization;

namespace Timescale.API.Models;

public class VehicleHourlyStats
{
    public DateTime TimeWindow { get; set; }
    public string VehicleId { get; set; } = string.Empty;
    public double? AvgSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? MinSpeed { get; set; }
    public double? AvgFuelLevel { get; set; }
    public double? MinFuelLevel { get; set; }
    public double? AvgEngineTemp { get; set; }
    public double? MaxEngineTemp { get; set; }
    public double? AvgEngineRpm { get; set; }
    public double? AvgBatteryVoltage { get; set; }
    public double LastOdometerReading { get; set; }
    public int ReadingCount { get; set; }
    public string? MostCommonEngineStatus { get; set; }
}