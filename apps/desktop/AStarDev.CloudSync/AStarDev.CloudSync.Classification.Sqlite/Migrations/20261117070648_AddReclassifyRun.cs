using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Classification.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddReclassifyRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReclassifyRun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    EngineVersion = table.Column<string>(type: "TEXT", nullable: false),
                    SeedVersion = table.Column<string>(type: "TEXT", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReclassifyRun", x => x.Id);
                    table.CheckConstraint("CK_ReclassifyRun_SingleRow", "\"Id\" = 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReclassifyRun");
        }
    }
}
