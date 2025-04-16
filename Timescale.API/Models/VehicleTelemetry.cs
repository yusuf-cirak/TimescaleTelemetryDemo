namespace Timescale.API.Models;

public sealed class VehicleTelemetry
{
    public const string IndexName = "vehicle-telemetry";
    public DateTime Timestamp { get; set; }
    public string VehicleId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; } // km/h
    public double? FuelLevel { get; set; } // percentage
    public double? EngineTemperature { get; set; } // celsius
    public int? EngineRpm { get; set; }
    public double? BatteryVoltage { get; set; }
    public string? EngineStatus { get; set; } // "RUNNING", "IDLE", "OFF"
    public double OdometerReading { get; set; } // km
}