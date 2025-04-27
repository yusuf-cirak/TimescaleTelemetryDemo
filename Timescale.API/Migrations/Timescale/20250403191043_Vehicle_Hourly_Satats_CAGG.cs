using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timescale.API.Migrations.Timescale
{
    /// <inheritdoc />
    public partial class Vehicle_Hourly_Satats_CAGG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create continuous aggregate view
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW ""VehicleHourlyStats""
                WITH (timescaledb.continuous,timescaledb.materialized_only=false) AS
                SELECT 
                    time_bucket('1 hour', ""Timestamp"") as ""TimeWindow"",
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
                GROUP BY time_bucket('1 hour', ""Timestamp""), ""VehicleId""
                WITH NO DATA;
            ");

            // Set refresh policy with minimum viable intervals
            migrationBuilder.Sql(@"
                SELECT add_continuous_aggregate_policy('""VehicleHourlyStats""',
                    start_offset => NULL,
                    end_offset => NULL,
                    schedule_interval => INTERVAL '1 minute');
            ");

            // Initial data population - executed outside transaction
            migrationBuilder.Sql(@"
                COMMIT;
                CALL refresh_continuous_aggregate('""VehicleHourlyStats""', NULL, NULL);
                BEGIN;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS \"VehicleHourlyStats\" CASCADE;");
        }
    }
}