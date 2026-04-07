using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861
namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    MicrosoftAccountId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AuthState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false, defaultValue: "Authenticated"),
                    ConsentDecisionMadeAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LocalSyncPath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false, defaultValue: ""),
                    SyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 15),
                    ConcurrencyLimit = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    StoreFileMetadata = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastSyncedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    IsSyncActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ThemeMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "en-GB"),
                    UserType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Casual"),
                    NotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "SyncStateRecords",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CheckpointJson = table.Column<string>(type: "TEXT", nullable: true),
                    DeltaToken = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStateRecords", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "SyncedFileMetadata",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteItemId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256Checksum = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastModifiedUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncedFileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncedFileMetadata_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictRecords_FilePath",
                table: "ConflictRecords",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_ConflictRecords_IsResolved_DetectedAt",
                table: "ConflictRecords",
                columns: new[] { "IsResolved", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncedFileMetadata_AccountId",
                table: "SyncedFileMetadata",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "ConflictRecords");

            migrationBuilder.DropTable(
                name: "SyncedFileMetadata");

            migrationBuilder.DropTable(
                name: "SyncStateRecords");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}

#pragma warning restore CA1861
