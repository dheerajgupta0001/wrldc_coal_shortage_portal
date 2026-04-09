using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class GenStnStgDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "GenStnStgs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "GenStnStgs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "GenStnStgs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedById",
                table: "GenStnStgs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GenStnStgs_CreatedById",
                table: "GenStnStgs",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_GenStnStgs_LastModifiedById",
                table: "GenStnStgs",
                column: "LastModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_GenStnStgs_AspNetUsers_CreatedById",
                table: "GenStnStgs",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GenStnStgs_AspNetUsers_LastModifiedById",
                table: "GenStnStgs",
                column: "LastModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GenStnStgs_AspNetUsers_CreatedById",
                table: "GenStnStgs");

            migrationBuilder.DropForeignKey(
                name: "FK_GenStnStgs_AspNetUsers_LastModifiedById",
                table: "GenStnStgs");

            migrationBuilder.DropIndex(
                name: "IX_GenStnStgs_CreatedById",
                table: "GenStnStgs");

            migrationBuilder.DropIndex(
                name: "IX_GenStnStgs_LastModifiedById",
                table: "GenStnStgs");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "GenStnStgs");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "GenStnStgs");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "GenStnStgs");

            migrationBuilder.DropColumn(
                name: "LastModifiedById",
                table: "GenStnStgs");
        }
    }
}
