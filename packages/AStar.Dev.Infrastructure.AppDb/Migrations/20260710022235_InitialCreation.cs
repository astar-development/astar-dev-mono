using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations;

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
            constraints: table => table.PrimaryKey("PK_Accounts", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Event",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                EventOccurredAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                DirectoryName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Handle = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Width = table.Column<int>(type: "INTEGER", nullable: true),
                Height = table.Column<int>(type: "INTEGER", nullable: true),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                FileCreated_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                FileLastModified_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Event", x => x.Id));

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
                IsInternet = table.Column<bool>(type: "INTEGER", nullable: false),
                IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
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
            name: "FileDetail",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FileHandle = table.Column<string>(type: "TEXT", nullable: false),
                FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                IsImage = table.Column<bool>(type: "INTEGER", nullable: false),
                DirectoryName = table.Column<string>(type: "TEXT", nullable: false),
                FileName = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_FileDetail", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ModelToIgnore",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ModelToIgnore", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ScrapeConfiguration",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ScrapeConfiguration", x => x.Id));

        migrationBuilder.CreateTable(
            name: "TagToIgnore",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                IgnoreImage = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_TagToIgnore", x => x.Id));

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
            name: "FileClassificationKeywords",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Keyword = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                IsFamous = table.Column<bool>(type: "INTEGER", nullable: false),
                IsInternet = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileClassificationKeywords", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileClassificationKeywords_FileClassificationCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "FileClassificationCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DeletionStatus",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                SoftDeleted_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                SoftDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                HardDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeletionStatus", x => x.Id);
                table.ForeignKey(
                    name: "FK_DeletionStatus_FileDetail_FileDetailId",
                    column: x => x.FileDetailId,
                    principalTable: "FileDetail",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "FileAccessDetail",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                DetailsLastUpdated_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                LastViewed_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                MoveRequired = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileAccessDetail", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileAccessDetail_FileDetail_FileDetailId",
                    column: x => x.FileDetailId,
                    principalTable: "FileDetail",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "FileClassifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileClassifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileClassifications_FileClassificationCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "FileClassificationCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_FileClassifications_FileDetail_FileDetailId",
                    column: x => x.FileDetailId,
                    principalTable: "FileDetail",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ImageDetail",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                Width = table.Column<int>(type: "INTEGER", nullable: true),
                Height = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImageDetail", x => x.Id);
                table.ForeignKey(
                    name: "FK_ImageDetail_FileDetail_FileDetailId",
                    column: x => x.FileDetailId,
                    principalTable: "FileDetail",
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
                SizeInBytes = table.Column<long>(type: "INTEGER", nullable: true),
                FileDetailId = table.Column<Guid>(type: "TEXT", nullable: true)
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
                table.ForeignKey(
                    name: "FK_SyncedItems_FileDetail_FileDetailId",
                    column: x => x.FileDetailId,
                    principalTable: "FileDetail",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ConnectionStrings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                Sqlite = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConnectionStrings", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConnectionStrings_ScrapeConfiguration_ScrapeConfigurationEntityId",
                    column: x => x.ScrapeConfigurationEntityId,
                    principalTable: "ScrapeConfiguration",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ScrapeDirectories",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                RootDirectory = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                BaseSaveDirectory = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                BaseDirectory = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                BaseDirectoryFamous = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                SubDirectoryName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScrapeDirectories", x => x.Id);
                table.ForeignKey(
                    name: "FK_ScrapeDirectories_ScrapeConfiguration_ScrapeConfigurationEntityId",
                    column: x => x.ScrapeConfigurationEntityId,
                    principalTable: "ScrapeConfiguration",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SearchConfiguration",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                BaseUrl = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                ApiKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                SearchString = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                TopWallpapers = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                SearchStringPrefix = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                SearchStringSuffix = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Subscriptions = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                ImagePauseInSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                StartingPageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                TotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                SubscriptionsStartingPageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                SubscriptionsTotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                TopWallpapersStartingPageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                TopWallpapersTotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                LoginUrl = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                UseHeadless = table.Column<bool>(type: "INTEGER", nullable: false),
                SlowMotionDelay = table.Column<float>(type: "REAL", nullable: true),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SearchConfiguration", x => x.Id);
                table.ForeignKey(
                    name: "FK_SearchConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                    column: x => x.ScrapeConfigurationEntityId,
                    principalTable: "ScrapeConfiguration",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserConfiguration",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                LoginEmailAddress = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Username = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Password = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                SessionCookie = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserConfiguration", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                    column: x => x.ScrapeConfigurationEntityId,
                    principalTable: "ScrapeConfiguration",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SearchCategories",
            columns: table => new
            {
                SearchConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastKnownImageCount = table.Column<int>(type: "INTEGER", nullable: false),
                LastPageVisited = table.Column<int>(type: "INTEGER", nullable: false),
                TotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SearchCategories", x => new { x.SearchConfigurationId, x.Id });
                table.ForeignKey(
                    name: "FK_SearchCategories_SearchConfiguration_SearchConfigurationId",
                    column: x => x.SearchConfigurationId,
                    principalTable: "SearchConfiguration",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConnectionStrings_ScrapeConfigurationEntityId",
            table: "ConnectionStrings",
            column: "ScrapeConfigurationEntityId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DeletionStatus_FileDetailId",
            table: "DeletionStatus",
            column: "FileDetailId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DriveStates_AccountId",
            table: "DriveStates",
            column: "AccountId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileAccessDetail_FileDetailId",
            table: "FileAccessDetail",
            column: "FileDetailId",
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
            name: "IX_FileClassificationKeywords_CategoryId_Keyword",
            table: "FileClassificationKeywords",
            columns: ["CategoryId", "Keyword"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileClassifications_CategoryId",
            table: "FileClassifications",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassifications_FileDetailId_CategoryId",
            table: "FileClassifications",
            columns: ["FileDetailId", "CategoryId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileDetail_DuplicateImages",
            table: "FileDetail",
            columns: ["IsImage", "FileSize"]);

        migrationBuilder.CreateIndex(
            name: "IX_FileDetail_FileHandle",
            table: "FileDetail",
            column: "FileHandle",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileDetail_FileSize",
            table: "FileDetail",
            column: "FileSize");

        migrationBuilder.CreateIndex(
            name: "IX_ImageDetail_FileDetailId",
            table: "ImageDetail",
            column: "FileDetailId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
            table: "ScrapeDirectories",
            column: "ScrapeConfigurationEntityId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SearchConfiguration_ScrapeConfigurationEntityId",
            table: "SearchConfiguration",
            column: "ScrapeConfigurationEntityId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SyncConflicts_AccountId_State",
            table: "SyncConflicts",
            columns: ["AccountId", "State"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_AccountId_IsFolder_RemotePath",
            table: "SyncedItems",
            columns: ["AccountId", "IsFolder", "RemotePath"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_AccountId_IsFolder_SizeInBytes",
            table: "SyncedItems",
            columns: ["AccountId", "IsFolder", "SizeInBytes"]);

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
            name: "IX_SyncedItems_FileDetailId",
            table: "SyncedItems",
            column: "FileDetailId");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_LocalPath",
            table: "SyncedItems",
            column: "LocalPath");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_RemotePath",
            table: "SyncedItems",
            column: "RemotePath");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_SizeInBytes",
            table: "SyncedItems",
            column: "SizeInBytes");

        migrationBuilder.CreateIndex(
            name: "IX_SyncJobs_AccountId_State",
            table: "SyncJobs",
            columns: ["AccountId", "State"]);

        migrationBuilder.CreateIndex(
            name: "IX_SyncRules_AccountId_RemotePath",
            table: "SyncRules",
            columns: ["AccountId", "RemotePath"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserConfiguration_ScrapeConfigurationEntityId",
            table: "UserConfiguration",
            column: "ScrapeConfigurationEntityId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ConnectionStrings");

        migrationBuilder.DropTable(
            name: "DeletionStatus");

        migrationBuilder.DropTable(
            name: "DriveStates");

        migrationBuilder.DropTable(
            name: "Event");

        migrationBuilder.DropTable(
            name: "FileAccessDetail");

        migrationBuilder.DropTable(
            name: "FileClassificationKeywords");

        migrationBuilder.DropTable(
            name: "FileClassifications");

        migrationBuilder.DropTable(
            name: "ImageDetail");

        migrationBuilder.DropTable(
            name: "ModelToIgnore");

        migrationBuilder.DropTable(
            name: "ScrapeDirectories");

        migrationBuilder.DropTable(
            name: "SearchCategories");

        migrationBuilder.DropTable(
            name: "SyncConflicts");

        migrationBuilder.DropTable(
            name: "SyncedItems");

        migrationBuilder.DropTable(
            name: "SyncJobs");

        migrationBuilder.DropTable(
            name: "SyncRules");

        migrationBuilder.DropTable(
            name: "TagToIgnore");

        migrationBuilder.DropTable(
            name: "UserConfiguration");

        migrationBuilder.DropTable(
            name: "FileClassificationCategories");

        migrationBuilder.DropTable(
            name: "SearchConfiguration");

        migrationBuilder.DropTable(
            name: "FileDetail");

        migrationBuilder.DropTable(
            name: "Accounts");

        migrationBuilder.DropTable(
            name: "ScrapeConfiguration");
    }
}
