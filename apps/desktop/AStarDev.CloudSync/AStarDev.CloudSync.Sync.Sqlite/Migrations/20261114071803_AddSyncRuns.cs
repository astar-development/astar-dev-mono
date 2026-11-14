using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    Outcome = table.Column<string>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_AccountId_StartedAtUtcTicks",
                table: "SyncRuns",
                columns: new[] { "AccountId", "StartedAtUtcTicks" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_FinishedAtUtcTicks",
                table: "SyncRuns",
                column: "FinishedAtUtcTicks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncRuns");
        }
    }
}
