using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class AddSyncJobsAndConflicts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<int>(
            name: "ConflictPolicy",
            table: "Accounts",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        _ = migrationBuilder.AddColumn<string>(
            name: "LocalSyncPath",
            table: "Accounts",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        _ = migrationBuilder.CreateTable(
            name: "SyncConflicts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                FolderId = table.Column<string>(type: "TEXT", nullable: false),
                RemoteItemId = table.Column<string>(type: "TEXT", nullable: false),
                RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                LocalModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                RemoteModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                LocalSize = table.Column<long>(type: "INTEGER", nullable: false),
                RemoteSize = table.Column<long>(type: "INTEGER", nullable: false),
                State = table.Column<int>(type: "INTEGER", nullable: false),
                Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                DetectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ResolvedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_SyncConflicts_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateTable(
            name: "SyncJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                FolderId = table.Column<string>(type: "TEXT", nullable: false),
                RemoteItemId = table.Column<string>(type: "TEXT", nullable: false),
                RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                Direction = table.Column<int>(type: "INTEGER", nullable: false),
                State = table.Column<int>(type: "INTEGER", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                DownloadUrl = table.Column<string>(type: "TEXT", nullable: true),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                RemoteModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                QueuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_SyncJobs", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_SyncJobs_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "IX_SyncConflicts_AccountId_State",
            table: "SyncConflicts",
            columns: ["AccountId", "State"]);

        _ = migrationBuilder.CreateIndex(
            name: "IX_SyncJobs_AccountId_State",
            table: "SyncJobs",
            columns: ["AccountId", "State"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "SyncConflicts");

        _ = migrationBuilder.DropTable(
            name: "SyncJobs");

        _ = migrationBuilder.DropColumn(
            name: "ConflictPolicy",
            table: "Accounts");

        _ = migrationBuilder.DropColumn(
            name: "LocalSyncPath",
            table: "Accounts");
    }
}
