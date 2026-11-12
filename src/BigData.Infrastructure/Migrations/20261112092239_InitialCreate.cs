using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherObservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParameterId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "DATETIME2(3)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "DATETIME2(3)", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherObservation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeatherObservation_CreatedAtUtc",
                table: "WeatherObservation",
                column: "CreatedAtUtc",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeatherObservation");
        }
    }
}
