using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class GenStnStgCreatedRemoveID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenStnStg_AspNetUsers_CreatedById",
                table: "GenStnStg");

            migrationBuilder.DropForeignKey(
                name: "FK_GenStnStg_AspNetUsers_LastModifiedById",
                table: "GenStnStg");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GenStnStg",
                table: "GenStnStg");

            migrationBuilder.DropIndex(
                name: "IX_GenStnStg_CreatedById",
                table: "GenStnStg");

            migrationBuilder.DropIndex(
                name: "IX_GenStnStg_LastModifiedById",
                table: "GenStnStg");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "GenStnStg");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "GenStnStg");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "GenStnStg");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "GenStnStg");

            migrationBuilder.RenameTable(
                name: "GenStnStg",
                newName: "GenStnStgs");

            migrationBuilder.RenameIndex(
                name: "IX_GenStnStg_Stage_StationName",
                table: "GenStnStgs",
                newName: "IX_GenStnStgs_Stage_StationName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GenStnStgs",
                table: "GenStnStgs",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GenStnStgs",
                table: "GenStnStgs");

            migrationBuilder.RenameTable(
                name: "GenStnStgs",
                newName: "GenStnStg");

            migrationBuilder.RenameIndex(
                name: "IX_GenStnStgs_Stage_StationName",
                table: "GenStnStg",
                newName: "IX_GenStnStg_Stage_StationName");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "GenStnStg",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "GenStnStg",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "GenStnStg",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedById",
                table: "GenStnStg",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GenStnStg",
                table: "GenStnStg",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GenStnStg_CreatedById",
                table: "GenStnStg",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GenStnStg_LastModifiedById",
                table: "GenStnStg",
                column: "LastModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_GenStnStg_AspNetUsers_CreatedById",
                table: "GenStnStg",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GenStnStg_AspNetUsers_LastModifiedById",
                table: "GenStnStg",
                column: "LastModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
