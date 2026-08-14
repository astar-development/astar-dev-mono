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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileAccessDetails");

            migrationBuilder.DropTable(
                name: "FileDeletionStatus");

            migrationBuilder.DropTable(
                name: "ImageDetails");

            migrationBuilder.DropTable(
                name: "Files");
        }
    }
}
