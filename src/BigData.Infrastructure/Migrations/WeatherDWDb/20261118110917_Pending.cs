using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigData.Infrastructure.Migrations.WeatherDWDb
{
    /// <inheritdoc />
    public partial class Pending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Station");

            migrationBuilder.DropTable(
                name: "WeatherObservation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Station",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StationId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Station", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherObservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "DATETIME2(3)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    ObservedAtUtc = table.Column<DateTime>(type: "DATETIME2(3)", nullable: false),
                    ParameterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StationId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherObservation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Station_StationId",
                table: "Station",
                column: "StationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservation_CreatedAtUtc",
                table: "WeatherObservation",
                column: "CreatedAtUtc",
                descending: new bool[0]);
        }
    }
}
