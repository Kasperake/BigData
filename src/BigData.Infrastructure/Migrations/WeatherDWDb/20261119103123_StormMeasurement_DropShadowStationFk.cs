using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigData.Infrastructure.Migrations.WeatherDWDb
{
    /// <inheritdoc />
    public partial class StormMeasurement_DropShadowStationFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StormMeasurements_Station_StationId1",
                table: "StormMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_StormMeasurements_StationId1",
                table: "StormMeasurements");

            migrationBuilder.DropColumn(
                name: "StationId1",
                table: "StormMeasurements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StationId1",
                table: "StormMeasurements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StormMeasurements_StationId1",
                table: "StormMeasurements",
                column: "StationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StormMeasurements_Station_StationId1",
                table: "StormMeasurements",
                column: "StationId1",
                principalTable: "Station",
                principalColumn: "Id");
        }
    }
}
