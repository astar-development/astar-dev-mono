using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class UnifyFileClassificationsSingleParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FileDetailId",
                table: "SyncedItems",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItems_FileDetailId",
                table: "SyncedItems",
                column: "FileDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_FileClassifications_CategoryId",
                table: "FileClassifications",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FileClassifications_FileDetailId_CategoryId",
                table: "FileClassifications",
                columns: new[] { "FileDetailId", "CategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SyncedItems_FileDetail_FileDetailId",
                table: "SyncedItems",
                column: "FileDetailId",
                principalTable: "FileDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                CREATE TEMP TABLE _legacy_map AS
                SELECT s."Id" AS SyncedItemId,
                       rtrim(rtrim(s."LocalPath", replace(replace(s."LocalPath", '/', ''), char(92), '')), '/' || char(92)) AS Dir,
                       substr(s."LocalPath", length(rtrim(s."LocalPath", replace(replace(s."LocalPath", '/', ''), char(92), ''))) + 1) AS Fn
                FROM "SyncedItems" s
                WHERE s."LocalPath" <> ''
                  AND EXISTS (SELECT 1 FROM "SyncedItemFileClassifications" c WHERE c."SyncedItemId" = s."Id");

                CREATE TEMP TABLE _new_files AS
                SELECT Dir, Fn,
                       row_number() OVER (ORDER BY Dir, Fn) AS Rn,
                       substr(H1, 1, 8) || '-' || substr(H1, 9, 4) || '-' || substr(H1, 13, 4) || '-' || substr(H1, 17, 4) || '-' || substr(H1, 21, 12) AS FdId,
                       substr(H2, 1, 8) || '-' || substr(H2, 9, 4) || '-' || substr(H2, 13, 4) || '-' || substr(H2, 17, 4) || '-' || substr(H2, 21, 12) AS ImgId
                FROM (
                    SELECT Dir, Fn, hex(randomblob(16)) AS H1, hex(randomblob(16)) AS H2
                    FROM (
                        SELECT DISTINCT m.Dir, m.Fn
                        FROM _legacy_map m
                        WHERE m.Fn <> ''
                          AND NOT EXISTS (SELECT 1 FROM "FileDetail" f WHERE f."DirectoryName" = m.Dir AND f."FileName" = m.Fn)
                    )
                );

                CREATE TEMP TABLE _bases AS
                SELECT (SELECT COALESCE(MAX("Id"), 0) FROM "FileAccessDetail") AS FaBase,
                       (SELECT COALESCE(MAX("Id"), 0) FROM "DeletionStatus") AS DsBase;

                INSERT INTO "FileAccessDetail" ("Id", "MoveRequired")
                SELECT (SELECT FaBase FROM _bases) + Rn, 0 FROM _new_files;

                INSERT INTO "DeletionStatus" ("Id")
                SELECT (SELECT DsBase FROM _bases) + Rn FROM _new_files;

                INSERT INTO "ImageDetail" ("Id")
                SELECT ImgId FROM _new_files;

                INSERT INTO "FileDetail" ("Id", "FileName", "DirectoryName", "FileHandle", "FileSize", "IsImage", "Width", "Height", "FileAccessDetailId", "ImageDetailId", "DeletionStatusId")
                SELECT FdId, Fn, Dir, Dir || '/' || Fn, 0, 0, 0, 0, (SELECT FaBase FROM _bases) + Rn, ImgId, (SELECT DsBase FROM _bases) + Rn
                FROM _new_files;

                CREATE TEMP TABLE _item_to_file AS
                SELECT m.SyncedItemId, f."Id" AS FdId
                FROM _legacy_map m
                JOIN "FileDetail" f ON f."DirectoryName" = m.Dir AND f."FileName" = m.Fn;

                INSERT OR IGNORE INTO "FileClassifications" ("FileDetailId", "CategoryId")
                SELECT "FileDetailId", "CategoryId"
                FROM "SyncedItemFileClassifications"
                WHERE "FileDetailId" IS NOT NULL;

                INSERT OR IGNORE INTO "FileClassifications" ("FileDetailId", "CategoryId")
                SELECT map.FdId, c."CategoryId"
                FROM "SyncedItemFileClassifications" c
                JOIN _item_to_file map ON map.SyncedItemId = c."SyncedItemId"
                WHERE c."SyncedItemId" IS NOT NULL;

                UPDATE "SyncedItems"
                SET "FileDetailId" = (SELECT FdId FROM _item_to_file WHERE _item_to_file.SyncedItemId = "SyncedItems"."Id")
                WHERE "Id" IN (SELECT SyncedItemId FROM _item_to_file);

                DROP TABLE _legacy_map;
                DROP TABLE _new_files;
                DROP TABLE _bases;
                DROP TABLE _item_to_file;
                """);

            migrationBuilder.DropTable(
                name: "SyncedItemFileClassifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncedItemFileClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileDetailId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SyncedItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncedItemFileClassifications", x => x.Id);
                    table.CheckConstraint("CK_SyncedItemFileClassifications_ExactlyOneParent", "(\"SyncedItemId\" IS NULL AND \"FileDetailId\" IS NOT NULL) OR (\"SyncedItemId\" IS NOT NULL AND \"FileDetailId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_SyncedItemFileClassifications_FileClassificationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FileClassificationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncedItemFileClassifications_FileDetail_FileDetailId",
                        column: x => x.FileDetailId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SyncedItemFileClassifications_SyncedItems_SyncedItemId",
                        column: x => x.SyncedItemId,
                        principalTable: "SyncedItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItemFileClassifications_CategoryId",
                table: "SyncedItemFileClassifications",
                column: "CategoryId");

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

            migrationBuilder.Sql("""
                INSERT INTO "SyncedItemFileClassifications" ("FileDetailId", "CategoryId")
                SELECT "FileDetailId", "CategoryId"
                FROM "FileClassifications";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_SyncedItems_FileDetail_FileDetailId",
                table: "SyncedItems");

            migrationBuilder.DropTable(
                name: "FileClassifications");

            migrationBuilder.DropIndex(
                name: "IX_SyncedItems_FileDetailId",
                table: "SyncedItems");

            migrationBuilder.DropColumn(
                name: "FileDetailId",
                table: "SyncedItems");
        }
    }
}
