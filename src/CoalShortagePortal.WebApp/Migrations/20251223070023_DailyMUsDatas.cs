using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class DailyMUsDatas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyMUsData_AspNetUsers_CreatedById",
                table: "DailyMUsData");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyMUsData_AspNetUsers_LastModifiedById",
                table: "DailyMUsData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DailyMUsData",
                table: "DailyMUsData");

            migrationBuilder.RenameTable(
                name: "DailyMUsData",
                newName: "DailyMUsDatas");

            migrationBuilder.RenameIndex(
                name: "IX_DailyMUsData_LastModifiedById",
                table: "DailyMUsDatas",
                newName: "IX_DailyMUsDatas_LastModifiedById");

            migrationBuilder.RenameIndex(
                name: "IX_DailyMUsData_DataDate_DailyMUs",
                table: "DailyMUsDatas",
                newName: "IX_DailyMUsDatas_DataDate_DailyMUs");

            migrationBuilder.RenameIndex(
                name: "IX_DailyMUsData_CreatedById",
                table: "DailyMUsDatas",
                newName: "IX_DailyMUsDatas_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DailyMUsDatas",
                table: "DailyMUsDatas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyMUsDatas_AspNetUsers_CreatedById",
                table: "DailyMUsDatas",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyMUsDatas_AspNetUsers_LastModifiedById",
                table: "DailyMUsDatas",
                column: "LastModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyMUsDatas_AspNetUsers_CreatedById",
                table: "DailyMUsDatas");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyMUsDatas_AspNetUsers_LastModifiedById",
                table: "DailyMUsDatas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DailyMUsDatas",
                table: "DailyMUsDatas");

            migrationBuilder.RenameTable(
                name: "DailyMUsDatas",
                newName: "DailyMUsData");

            migrationBuilder.RenameIndex(
                name: "IX_DailyMUsDatas_LastModifiedById",
                table: "DailyMUsData",
                newName: "IX_DailyMUsData_LastModifiedById");

            migrationBuilder.RenameIndex(
                name: "IX_DailyMUsDatas_DataDate_DailyMUs",
                table: "DailyMUsData",
                newName: "IX_DailyMUsData_DataDate_DailyMUs");

            migrationBuilder.RenameIndex(
                name: "IX_DailyMUsDatas_CreatedById",
                table: "DailyMUsData",
                newName: "IX_DailyMUsData_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DailyMUsData",
                table: "DailyMUsData",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyMUsData_AspNetUsers_CreatedById",
                table: "DailyMUsData",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyMUsData_AspNetUsers_LastModifiedById",
                table: "DailyMUsData",
                column: "LastModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
