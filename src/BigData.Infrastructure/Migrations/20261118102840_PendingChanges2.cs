using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigData.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "StormMeasurement");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StormMeasurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StationId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StormMeasurement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StormMeasurementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parameters_StormMeasurement_StormMeasurementId",
                        column: x => x.StormMeasurementId,
                        principalTable: "StormMeasurement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_StormMeasurementId",
                table: "Parameters",
                column: "StormMeasurementId");
        }
    }
}
