using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timescale.API.Migrations.Timescale
{
    /// <inheritdoc />
    public partial class Remove_PK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VehicleTelemetries",
                table: "VehicleTelemetries");

            // migrationBuilder.CreateIndex(
            //     name: "IX_VehicleTelemetries_VehicleId_Timestamp",
            //     table: "VehicleTelemetries",
            //     columns: new[] { "VehicleId", "Timestamp" },
            //     descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropIndex(
            //     name: "IX_VehicleTelemetries_VehicleId_Timestamp",
            //     table: "VehicleTelemetries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VehicleTelemetries",
                table: "VehicleTelemetries",
                columns: new[] { "VehicleId", "Timestamp" });
        }
    }
}
