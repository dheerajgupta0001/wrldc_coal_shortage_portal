using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class GeneratingStationIdToUserDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeneratingStationId",
                table: "UserDetails",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratingStationId",
                table: "UserDetails");
        }
    }
}
