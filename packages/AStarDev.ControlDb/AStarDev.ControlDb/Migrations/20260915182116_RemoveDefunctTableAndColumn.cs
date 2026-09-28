using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefunctTableAndColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionStrings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "SearchConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: true,
                collation: "NOCASE");

            migrationBuilder.CreateTable(
                name: "ConnectionStrings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sqlite = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionStrings_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionStrings_ScrapeConfigurationEntityId",
                table: "ConnectionStrings",
                column: "ScrapeConfigurationEntityId",
                unique: true);
        }
    }
}
