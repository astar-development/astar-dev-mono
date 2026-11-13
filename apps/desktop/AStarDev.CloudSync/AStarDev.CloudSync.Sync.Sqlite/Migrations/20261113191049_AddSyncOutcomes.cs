using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncOutcomes",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Conflicts = table.Column<int>(type: "INTEGER", nullable: false),
                    HeldDeletions = table.Column<int>(type: "INTEGER", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOutcomes", x => x.AccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncOutcomes");
        }
    }
}
