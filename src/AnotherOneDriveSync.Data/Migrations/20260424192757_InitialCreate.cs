using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnotherOneDriveSync.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DriveId = table.Column<string>(type: "TEXT", nullable: false),
                    FolderId = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    LastSyncTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ETag = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenCacheEntries",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenCacheEntries", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "DriveItemMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<string>(type: "TEXT", nullable: true),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastModifiedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    SyncFolderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriveItemMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriveItemMetadata_SyncFolders_SyncFolderId",
                        column: x => x.SyncFolderId,
                        principalTable: "SyncFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncFolderStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SyncFolderId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncFolderStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncFolderStatuses_SyncFolders_SyncFolderId",
                        column: x => x.SyncFolderId,
                        principalTable: "SyncFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriveItemMetadata_SyncFolderId",
                table: "DriveItemMetadata",
                column: "SyncFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncFolderStatuses_SyncFolderId",
                table: "SyncFolderStatuses",
                column: "SyncFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriveItemMetadata");

            migrationBuilder.DropTable(
                name: "SyncFolderStatuses");

            migrationBuilder.DropTable(
                name: "TokenCacheEntries");

            migrationBuilder.DropTable(
                name: "SyncFolders");
        }
    }
}
