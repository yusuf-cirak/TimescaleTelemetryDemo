using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timescale.API.Migrations.Timescale
{
    public partial class Initial : Migration
    {
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

            // Make VehicleTelemetries a hypertable
            migrationBuilder.Sql("""
                SELECT create_hypertable(
                    '"VehicleTelemetries"',
                    'Timestamp',
                    chunk_time_interval => INTERVAL '1 month',
                    if_not_exists => true,
                    migrate_data => true
                );
            """);

            // Add retention policy
            migrationBuilder.Sql("""
                SELECT add_retention_policy(
                    '"VehicleTelemetries"',
                    INTERVAL '1 year',
                    if_not_exists => true
                );
            """);

            // Enable compression
            migrationBuilder.Sql("""
                ALTER TABLE "VehicleTelemetries"
                SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = '"VehicleId"',
                    timescaledb.compress_orderby = '"Timestamp" DESC'
                );
            """);

            // Add compression policy
            migrationBuilder.Sql("""
                SELECT add_compression_policy(
                    '"VehicleTelemetries"',
                    INTERVAL '7 days',
                    if_not_exists => true
                );
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove compression policy
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('"VehicleTelemetries"') IS NOT NULL THEN
                        PERFORM remove_compression_policy('"VehicleTelemetries"', if_exists => true);
                    END IF;
                END
                $$;
            """);

            // Remove retention policy
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('"VehicleTelemetries"') IS NOT NULL THEN
                        PERFORM remove_retention_policy('"VehicleTelemetries"', if_exists => true);
                    END IF;
                END
                $$;
            """);

            // Drop table (handled by EF Core with correct casing)
            migrationBuilder.DropTable(name: "VehicleTelemetries");
        }
    }
}
