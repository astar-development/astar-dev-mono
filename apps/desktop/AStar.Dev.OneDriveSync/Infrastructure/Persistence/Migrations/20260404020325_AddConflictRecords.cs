using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConflictRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConflictRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    AccountDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LocalLastModified = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteLastModified = table.Column<long>(type: "INTEGER", nullable: false),
                    ConflictType = table.Column<int>(type: "INTEGER", maxLength: 32, nullable: false),
                    DetectedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AppliedStrategy = table.Column<int>(type: "INTEGER", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictRecords_FilePath",
                table: "ConflictRecords",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictRecords_IsResolved_DetectedAt",
                table: "ConflictRecords",
                columns: ["IsResolved", "DetectedAt"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConflictRecords");
        }
    }
}
