using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations;

/// <inheritdoc />
public partial class RecreateDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                AccentIndex = table.Column<int>(type: "INTEGER", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                LastSyncedAt = table.Column<long>(type: "INTEGER", nullable: true),
                DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", nullable: false),
                QuotaTotal = table.Column<long>(type: "INTEGER", nullable: false),
                QuotaUsed = table.Column<long>(type: "INTEGER", nullable: false),
                ConflictPolicy = table.Column<int>(type: "INTEGER", nullable: false),
                LocalSyncPath = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Accounts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FileClassificationCategories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Level = table.Column<int>(type: "INTEGER", nullable: false),
                ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                IsFamous = table.Column<bool>(type: "INTEGER", nullable: false),
                IsInternet = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileClassificationCategories", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileClassificationCategories_FileClassificationCategories_ParentId",
                    column: x => x.ParentId,
                    principalTable: "FileClassificationCategories",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "DriveStates",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                DeltaLink = table.Column<string>(type: "TEXT", nullable: true),
                LastSyncStartedAt = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DriveStates", x => x.Id);
                table.ForeignKey(
                    name: "FK_DriveStates_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SyncConflicts",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                FolderId = table.Column<string>(type: "TEXT", nullable: false),
                RemoteItemId = table.Column<string>(type: "TEXT", nullable: false),
                RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                LocalModified_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                RemoteModified_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                LocalSize = table.Column<long>(type: "INTEGER", nullable: false),
                RemoteSize = table.Column<long>(type: "INTEGER", nullable: false),
                State = table.Column<int>(type: "INTEGER", nullable: false),
                Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                DetectedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                ResolvedAt = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncConflicts_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SyncedItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                RemoteItemId = table.Column<string>(type: "TEXT", nullable: false),
                RemoteParentId = table.Column<string>(type: "TEXT", nullable: false),
                RemotePath = table.Column<string>(type: "TEXT", nullable: false),
                LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                RemoteModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ETag = table.Column<string>(type: "TEXT", nullable: true),
                CTag = table.Column<string>(type: "TEXT", nullable: true),
                SizeInBytes = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncedItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncedItems_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SyncJobs",
            columns: table => new
            {
                Id = table.Column<byte[]>(type: "BLOB", nullable: false),
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
                RemoteModified_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                QueuedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                CompletedAt = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncJobs_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SyncRules",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                RemotePath = table.Column<string>(type: "TEXT", nullable: false),
                RuleType = table.Column<int>(type: "INTEGER", nullable: false),
                RemoteItemId = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncRules", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncRules_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SyncedItemFileClassifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SyncedItemId = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncedItemFileClassifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncedItemFileClassifications_FileClassificationCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "FileClassificationCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_SyncedItemFileClassifications_SyncedItems_SyncedItemId",
                    column: x => x.SyncedItemId,
                    principalTable: "SyncedItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DriveStates_AccountId",
            table: "DriveStates",
            column: "AccountId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationCategories_Name",
            table: "FileClassificationCategories",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationCategories_ParentId_Name",
            table: "FileClassificationCategories",
            columns: ["ParentId", "Name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SyncConflicts_AccountId_State",
            table: "SyncConflicts",
            columns: ["AccountId", "State"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItemFileClassifications_CategoryId",
            table: "SyncedItemFileClassifications",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItemFileClassifications_SyncedItemId_CategoryId",
            table: "SyncedItemFileClassifications",
            columns: ["SyncedItemId", "CategoryId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_AccountId_LocalPath",
            table: "SyncedItems",
            columns: ["AccountId", "LocalPath"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_AccountId_RemoteItemId",
            table: "SyncedItems",
            columns: ["AccountId", "RemoteItemId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_AccountId_SizeInBytes",
            table: "SyncedItems",
            columns: ["AccountId", "SizeInBytes"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncJobs_AccountId_State",
            table: "SyncJobs",
            columns: ["AccountId", "State"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncRules_AccountId_RemotePath",
            table: "SyncRules",
            columns: ["AccountId", "RemotePath"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DriveStates");

        migrationBuilder.DropTable(
            name: "SyncConflicts");

        migrationBuilder.DropTable(
            name: "SyncedItemFileClassifications");

        migrationBuilder.DropTable(
            name: "SyncJobs");

        migrationBuilder.DropTable(
            name: "SyncRules");

        migrationBuilder.DropTable(
            name: "FileClassificationCategories");

        migrationBuilder.DropTable(
            name: "SyncedItems");

        migrationBuilder.DropTable(
            name: "Accounts");
    }
}
