using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class uniq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyMUsDatas_DataDate",
                table: "DailyMUsDatas");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMUsDatas_DataDate_StationName",
                table: "DailyMUsDatas",
                columns: new[] { "DataDate", "StationName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyMUsDatas_DataDate_StationName",
                table: "DailyMUsDatas");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMUsDatas_DataDate",
                table: "DailyMUsDatas",
                column: "DataDate",
                unique: true);
        }
    }
}
