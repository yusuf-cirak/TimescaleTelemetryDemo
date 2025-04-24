using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timescale.API.Migrations.Native
{
    /// <inheritdoc />
    public partial class Create_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VehicleTelemetries_VehicleId_Timestamp",
                table: "VehicleTelemetries",
                columns: new[] { "VehicleId", "Timestamp" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleTelemetries_VehicleId_Timestamp",
                table: "VehicleTelemetries");
        }
    }
}
