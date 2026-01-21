using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class Unique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyMUsDatas_DataDate_DailyMUs",
                table: "DailyMUsDatas");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMUsDatas_DataDate_DailyMUs_StationName",
                table: "DailyMUsDatas",
                columns: new[] { "DataDate", "DailyMUs", "StationName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyMUsDatas_DataDate_DailyMUs_StationName",
                table: "DailyMUsDatas");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMUsDatas_DataDate_DailyMUs",
                table: "DailyMUsDatas",
                columns: new[] { "DataDate", "DailyMUs" },
                unique: true);
        }
    }
}
