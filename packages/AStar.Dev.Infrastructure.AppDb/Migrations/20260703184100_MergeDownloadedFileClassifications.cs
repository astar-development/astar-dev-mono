using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class MergeDownloadedFileClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncedItemFileClassifications_SyncedItemId_CategoryId",
                table: "SyncedItemFileClassifications");

            migrationBuilder.AlterColumn<int>(
                name: "SyncedItemId",
                table: "SyncedItemFileClassifications",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<Guid>(
                name: "FileDetailId",
                table: "SyncedItemFileClassifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItemFileClassifications_FileDetailId_CategoryId",
                table: "SyncedItemFileClassifications",
                columns: new[] { "FileDetailId", "CategoryId" },
                unique: true,
                filter: "\"FileDetailId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItemFileClassifications_SyncedItemId_CategoryId",
                table: "SyncedItemFileClassifications",
                columns: new[] { "SyncedItemId", "CategoryId" },
                unique: true,
                filter: "\"SyncedItemId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SyncedItemFileClassifications_ExactlyOneParent",
                table: "SyncedItemFileClassifications",
                sql: "(\"SyncedItemId\" IS NULL AND \"FileDetailId\" IS NOT NULL) OR (\"SyncedItemId\" IS NOT NULL AND \"FileDetailId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_SyncedItemFileClassifications_FileDetail_FileDetailId",
                table: "SyncedItemFileClassifications",
                column: "FileDetailId",
                principalTable: "FileDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                INSERT INTO "SyncedItemFileClassifications" ("FileDetailId", "CategoryId")
                SELECT "FileDetailId", "CategoryId"
                FROM "DownloadedFileClassifications";
                """);

            migrationBuilder.DropTable(
                name: "DownloadedFileClassifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadedFileClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadedFileClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadedFileClassifications_FileClassificationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FileClassificationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DownloadedFileClassifications_FileDetail_FileDetailId",
                        column: x => x.FileDetailId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "DownloadedFileClassifications" ("FileDetailId", "CategoryId")
                SELECT "FileDetailId", "CategoryId"
                FROM "SyncedItemFileClassifications"
                WHERE "FileDetailId" IS NOT NULL;

                DELETE FROM "SyncedItemFileClassifications" WHERE "FileDetailId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_SyncedItemFileClassifications_FileDetail_FileDetailId",
                table: "SyncedItemFileClassifications");

            migrationBuilder.DropIndex(
                name: "IX_SyncedItemFileClassifications_FileDetailId_CategoryId",
                table: "SyncedItemFileClassifications");

            migrationBuilder.DropIndex(
                name: "IX_SyncedItemFileClassifications_SyncedItemId_CategoryId",
                table: "SyncedItemFileClassifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SyncedItemFileClassifications_ExactlyOneParent",
                table: "SyncedItemFileClassifications");

            migrationBuilder.DropColumn(
                name: "FileDetailId",
                table: "SyncedItemFileClassifications");

            migrationBuilder.AlterColumn<int>(
                name: "SyncedItemId",
                table: "SyncedItemFileClassifications",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItemFileClassifications_SyncedItemId_CategoryId",
                table: "SyncedItemFileClassifications",
                columns: new[] { "SyncedItemId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DownloadedFileClassifications_CategoryId",
                table: "DownloadedFileClassifications",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadedFileClassifications_FileDetailId_CategoryId",
                table: "DownloadedFileClassifications",
                columns: new[] { "FileDetailId", "CategoryId" },
                unique: true);
        }
    }
}
