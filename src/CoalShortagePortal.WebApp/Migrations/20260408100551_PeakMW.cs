using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class PeakMW : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayPeakMW",
                table: "DailyMUsDatas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DayPeakMWTime",
                table: "DailyMUsDatas",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "MinGeneration",
                table: "DailyMUsDatas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MinGenerationTime",
                table: "DailyMUsDatas",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "OffPeakMW",
                table: "DailyMUsDatas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OffPeakMWTime",
                table: "DailyMUsDatas",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "PeakMW",
                table: "DailyMUsDatas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PeakMWTime",
                table: "DailyMUsDatas",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayPeakMW",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "DayPeakMWTime",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "MinGeneration",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "MinGenerationTime",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "OffPeakMW",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "OffPeakMWTime",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "PeakMW",
                table: "DailyMUsDatas");

            migrationBuilder.DropColumn(
                name: "PeakMWTime",
                table: "DailyMUsDatas");
        }
    }
}
