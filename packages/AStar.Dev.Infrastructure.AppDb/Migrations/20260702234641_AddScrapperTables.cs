using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapperTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeletionStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftDeleted_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    SoftDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    HardDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionStatus", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileAccessDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DetailsLastUpdated_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    LastViewed_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    MoveRequired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccessDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileNamePart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileNamePart", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelToIgnore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelToIgnore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapedTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedTag", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagToIgnore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileHandle = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    IsImage = table.Column<bool>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    FileAccessDetailId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeletionStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectoryName = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDetail_DeletionStatus_DeletionStatusId",
                        column: x => x.DeletionStatusId,
                        principalTable: "DeletionStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileDetail_FileAccessDetail_FileAccessDetailId",
                        column: x => x.FileAccessDetailId,
                        principalTable: "FileAccessDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileDetail_ImageDetail_ImageDetailId",
                        column: x => x.ImageDetailId,
                        principalTable: "ImageDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_FileDetail_DeletionStatusId",
                table: "FileDetail",
                column: "DeletionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_DuplicateImages",
                table: "FileDetail",
                columns: new[] { "IsImage", "FileSize" });

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileAccessDetailId",
                table: "FileDetail",
                column: "FileAccessDetailId");

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
                name: "IX_FileDetail_ImageDetailId",
                table: "FileDetail",
                column: "ImageDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedTag_Value",
                table: "ScrapedTag",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchConfiguration_ScrapeConfigurationEntityId",
                table: "SearchConfiguration",
                column: "ScrapeConfigurationEntityId",
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
                name: "Event");

            migrationBuilder.DropTable(
                name: "FileDetail");

            migrationBuilder.DropTable(
                name: "FileNamePart");

            migrationBuilder.DropTable(
                name: "ModelToIgnore");

            migrationBuilder.DropTable(
                name: "ScrapeDirectories");

            migrationBuilder.DropTable(
                name: "ScrapedTag");

            migrationBuilder.DropTable(
                name: "SearchCategories");

            migrationBuilder.DropTable(
                name: "TagToIgnore");

            migrationBuilder.DropTable(
                name: "UserConfiguration");

            migrationBuilder.DropTable(
                name: "DeletionStatus");

            migrationBuilder.DropTable(
                name: "FileAccessDetail");

            migrationBuilder.DropTable(
                name: "ImageDetail");

            migrationBuilder.DropTable(
                name: "SearchConfiguration");

            migrationBuilder.DropTable(
                name: "ScrapeConfiguration");
        }
    }
}
