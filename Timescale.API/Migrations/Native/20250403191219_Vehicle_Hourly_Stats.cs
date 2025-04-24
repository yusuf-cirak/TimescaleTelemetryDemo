using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timescale.API.Migrations.Native
{
    /// <inheritdoc />
    public partial class Vehicle_Hourly_Stats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create materialized view
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW ""VehicleHourlyStats"" AS
                SELECT 
                    date_trunc('hour', ""Timestamp"") as ""TimeWindow"",
                    ""VehicleId"",
                    AVG(""Speed"")::double precision as ""AvgSpeed"",
                    MAX(""Speed"")::double precision as ""MaxSpeed"",
                    MIN(""Speed"")::double precision as ""MinSpeed"",
                    AVG(""FuelLevel"")::double precision as ""AvgFuelLevel"",
                    MIN(""FuelLevel"")::double precision as ""MinFuelLevel"",
                    AVG(""EngineTemperature"")::double precision as ""AvgEngineTemp"",
                    MAX(""EngineTemperature"")::double precision as ""MaxEngineTemp"",
                    AVG(""EngineRpm"")::double precision as ""AvgEngineRpm"",
                    AVG(""BatteryVoltage"")::double precision as ""AvgBatteryVoltage"",
                    MAX(""OdometerReading"")::double precision as ""LastOdometerReading"",
                    COUNT(*) as ""ReadingCount"",
                    MODE() WITHIN GROUP (ORDER BY ""EngineStatus"") as ""MostCommonEngineStatus""
                FROM ""VehicleTelemetries""
                GROUP BY date_trunc('hour', ""Timestamp""), ""VehicleId""
            ");
            
            migrationBuilder.Sql(@"
            CREATE UNIQUE INDEX idx_vehicle_hourly_stats_vehicleid_timewindow ON ""VehicleHourlyStats"" (""VehicleId"", ""TimeWindow"");
        ");

            // Create a function to refresh the materialized view
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION refresh_vehicle_stats()
                RETURNS trigger AS $$
                BEGIN
                    -- Use CONCURRENTLY to allow reads during refresh
                    REFRESH MATERIALIZED VIEW CONCURRENTLY ""VehicleHourlyStats"";
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create a trigger to refresh the view
            migrationBuilder.Sql(@"
                CREATE TRIGGER refresh_vehicle_stats_trigger
                AFTER INSERT OR UPDATE OR DELETE ON ""VehicleTelemetries""
                FOR EACH STATEMENT
                EXECUTE FUNCTION refresh_vehicle_stats();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove trigger and function
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS refresh_vehicle_stats_trigger ON \"VehicleTelemetries\"");
            
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS refresh_vehicle_stats()");
            
            // Remove materialized view and index
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS \"VehicleHourlyStats\"");
        }
    }
}