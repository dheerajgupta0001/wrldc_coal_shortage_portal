using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CoalShortagePortal.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitAuxiliaryConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitAuxiliaryConsumptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GeneratingStationId = table.Column<int>(type: "integer", nullable: false),
                    UnitNumber = table.Column<int>(type: "integer", nullable: false),
                    InstalledCapacityMw = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    AuxiliaryConsumptionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedById = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedById = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitAuxiliaryConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitAuxiliaryConsumptions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UnitAuxiliaryConsumptions_AspNetUsers_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitAuxiliaryConsumptions_CreatedById",
                table: "UnitAuxiliaryConsumptions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_UnitAuxiliaryConsumptions_GeneratingStationId_UnitNumber_Cr~",
                table: "UnitAuxiliaryConsumptions",
                columns: new[] { "GeneratingStationId", "UnitNumber", "Created" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitAuxiliaryConsumptions_LastModifiedById",
                table: "UnitAuxiliaryConsumptions",
                column: "LastModifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitAuxiliaryConsumptions");
        }
    }
}
