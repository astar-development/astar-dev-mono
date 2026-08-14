using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.UniqueConstraint("AK_Files_Path_Name", x => new { x.Path, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "ScrapeConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileAccessDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DetailsLastUpdated_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    LastViewed_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    MoveRequired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccessDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAccessDetails_Files_FileEntityId",
                        column: x => x.FileEntityId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileDeletionStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SoftDeleted_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    SoftDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    HardDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDeletionStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDeletionStatus_Files_FileEntityId",
                        column: x => x.FileEntityId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageDetails_Files_FileEntityId",
                        column: x => x.FileEntityId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionStrings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sqlite = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionStrings_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeDirectories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootDirectory = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    BaseSaveDirectory = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    BaseDirectory = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    BaseDirectoryFamous = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SubDirectoryName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeDirectories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapeDirectories_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchTerm = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Category = table.Column<string>(type: "TEXT", nullable: true, collation: "NOCASE"),
                    MaxResults = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchConfigurations_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Username = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Password = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SessionCookie = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConfigurations_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionStrings_ScrapeConfigurationEntityId",
                table: "ConnectionStrings",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessDetails_FileEntityId",
                table: "FileAccessDetails",
                column: "FileEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDeletionStatus_FileEntityId",
                table: "FileDeletionStatus",
                column: "FileEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImageDetails_FileEntityId",
                table: "ImageDetails",
                column: "FileEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchConfigurations_ScrapeConfigurationEntityId",
                table: "SearchConfigurations",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConfigurations_ScrapeConfigurationEntityId",
                table: "UserConfigurations",
                column: "ScrapeConfigurationEntityId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionStrings");

            migrationBuilder.DropTable(
                name: "FileAccessDetails");

            migrationBuilder.DropTable(
                name: "FileDeletionStatus");

            migrationBuilder.DropTable(
                name: "ImageDetails");

            migrationBuilder.DropTable(
                name: "ScrapeDirectories");

            migrationBuilder.DropTable(
                name: "SearchConfigurations");

            migrationBuilder.DropTable(
                name: "UserConfigurations");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "ScrapeConfigurations");
        }
    }
}
