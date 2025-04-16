using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timescale.API.Migrations.Timescale
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleTelemetries",
                columns: table => new
                {
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VehicleId = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Speed = table.Column<double>(type: "double precision", nullable: true),
                    FuelLevel = table.Column<double>(type: "double precision", nullable: true),
                    EngineTemperature = table.Column<double>(type: "double precision", nullable: true),
                    EngineRpm = table.Column<int>(type: "integer", nullable: true),
                    BatteryVoltage = table.Column<double>(type: "double precision", nullable: true),
                    EngineStatus = table.Column<string>(type: "text", nullable: true),
                    OdometerReading = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleTelemetries", x => new { x.VehicleId, x.Timestamp });
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTelemetries_VehicleId_Timestamp",
                table: "VehicleTelemetries",
                columns: new[] { "VehicleId", "Timestamp" },
                descending: new[] { false, true });
            
            
            migrationBuilder.Sql(
                "SELECT create_hypertable( '\"VehicleTelemetries\"', by_range('Timestamp', INTERVAL '1 day'));"
            );
            migrationBuilder.Sql(
                "SELECT add_retention_policy( '\"VehicleTelemetries\"', INTERVAL '1 month');"
            );
            
            // Enable compression with specific settings
            migrationBuilder.Sql(
                "ALTER TABLE \"VehicleTelemetries\" SET (timescaledb.compress, timescaledb.compress_segmentby = '\"VehicleId\"', timescaledb.compress_orderby = '\"Timestamp\" DESC');"
            );
            
            // Add compression policy (compress data older than 1 day)
            migrationBuilder.Sql(
                "SELECT add_compression_policy('\"VehicleTelemetries\"', INTERVAL '1 day', if_not_exists => true);"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleTelemetries");

            migrationBuilder.Sql(
                """
                DO
                $do$
                    BEGIN
                        IF (select to_regclass('"VehicleTelemetries"') is not null) THEN
                            PERFORM remove_retention_policy('"VehicleTelemetries"', if_exists := true);
                        ELSE
                            PERFORM 'NOOP' as Noop;
                        END IF;
                    END
                $do$
                """
            );
            
            // Remove compression policy first
            migrationBuilder.Sql(
                "SELECT remove_compression_policy('\"VehicleTelemetries\"', if_exists => true);"
            );

            // Remove retention policy
            migrationBuilder.Sql(
                "DO $$ BEGIN IF (select to_regclass('\"VehicleTelemetries\"') is not null) THEN PERFORM remove_retention_policy('\"VehicleTelemetries\"', if_exists := true); END IF; END $$;"
            );
        }
    }
}
