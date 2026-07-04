using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class RepairClassificationData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TEMP TABLE _dup_categories AS
                SELECT DupId, KeeperId FROM (
                    SELECT d."Id" AS DupId,
                           (SELECT k."Id" FROM "FileClassificationCategories" k
                            WHERE k."ParentId" IS NOT NULL AND k."Id" <> d."Id" AND k."Name" = d."Name" COLLATE NOCASE
                            ORDER BY k."Level" DESC, k."Id" LIMIT 1) AS KeeperId
                    FROM "FileClassificationCategories" d
                    WHERE d."Level" = 3 AND d."ParentId" IS NULL
                )
                WHERE KeeperId IS NOT NULL;

                INSERT OR IGNORE INTO "FileClassifications" ("FileDetailId", "CategoryId")
                SELECT fc."FileDetailId", m.KeeperId
                FROM "FileClassifications" fc
                JOIN _dup_categories m ON m.DupId = fc."CategoryId";

                DELETE FROM "FileClassifications" WHERE "CategoryId" IN (SELECT DupId FROM _dup_categories);

                INSERT OR IGNORE INTO "FileClassificationKeywords" ("Keyword", "CategoryId")
                SELECT k."Keyword", m.KeeperId
                FROM "FileClassificationKeywords" k
                JOIN _dup_categories m ON m.DupId = k."CategoryId";

                DELETE FROM "FileClassificationKeywords" WHERE "CategoryId" IN (SELECT DupId FROM _dup_categories);

                UPDATE OR IGNORE "FileClassificationCategories"
                SET "ParentId" = (SELECT KeeperId FROM _dup_categories WHERE DupId = "FileClassificationCategories"."ParentId")
                WHERE "ParentId" IN (SELECT DupId FROM _dup_categories);

                DELETE FROM "FileClassificationCategories" WHERE "Id" IN (SELECT DupId FROM _dup_categories);

                CREATE TEMP TABLE _twin_files AS
                SELECT DupId, KeepId, DiskName FROM (
                    SELECT dup."Id" AS DupId, keep."Id" AS KeepId, dup."FileName" AS DiskName,
                           row_number() OVER (PARTITION BY dup."Id" ORDER BY length(keep."FileName") DESC, keep."Id") AS Rn
                    FROM "FileDetail" dup
                    JOIN "FileDetail" keep ON keep."DirectoryName" = dup."DirectoryName"
                       AND keep."Id" <> dup."Id"
                       AND length(dup."FileName") > length(keep."FileName")
                       AND substr(dup."FileName", -length(keep."FileName") - 1) = ' ' || keep."FileName"
                )
                WHERE Rn = 1;

                CREATE TEMP TABLE _twin_owned AS
                SELECT f."FileAccessDetailId" AS FaId, f."DeletionStatusId" AS DsId, f."ImageDetailId" AS ImgId
                FROM "FileDetail" f
                WHERE f."Id" IN (SELECT DupId FROM _twin_files);

                INSERT OR IGNORE INTO "FileClassifications" ("FileDetailId", "CategoryId")
                SELECT m.KeepId, fc."CategoryId"
                FROM "FileClassifications" fc
                JOIN _twin_files m ON m.DupId = fc."FileDetailId";

                DELETE FROM "FileClassifications" WHERE "FileDetailId" IN (SELECT DupId FROM _twin_files);

                UPDATE "SyncedItems"
                SET "FileDetailId" = (SELECT KeepId FROM _twin_files WHERE DupId = "SyncedItems"."FileDetailId")
                WHERE "FileDetailId" IN (SELECT DupId FROM _twin_files);

                DELETE FROM "FileDetail" WHERE "Id" IN (SELECT DupId FROM _twin_files);

                UPDATE "FileDetail"
                SET "FileName" = (SELECT DiskName FROM _twin_files WHERE KeepId = "FileDetail"."Id")
                WHERE "Id" IN (SELECT KeepId FROM _twin_files);

                DELETE FROM "FileAccessDetail" WHERE "Id" IN (SELECT FaId FROM _twin_owned);
                DELETE FROM "DeletionStatus" WHERE "Id" IN (SELECT DsId FROM _twin_owned);
                DELETE FROM "ImageDetail" WHERE "Id" IN (SELECT ImgId FROM _twin_owned);

                DELETE FROM "FileClassifications"
                WHERE "CategoryId" IN (SELECT "Id" FROM "FileClassificationCategories" WHERE "Name" = 'Unclassified' AND "Level" = 1)
                  AND EXISTS (SELECT 1 FROM "FileClassifications" o
                              WHERE o."FileDetailId" = "FileClassifications"."FileDetailId"
                                AND o."CategoryId" NOT IN (SELECT "Id" FROM "FileClassificationCategories" WHERE "Name" = 'Unclassified' AND "Level" = 1));

                CREATE TEMP TABLE _orphan_self_dups AS
                SELECT o."Id" AS DupId,
                       (SELECT MIN(k."Id") FROM "FileClassificationCategories" k
                        WHERE k."Level" = 3 AND k."ParentId" IS NULL AND k."Name" = o."Name" COLLATE NOCASE) AS KeeperId
                FROM "FileClassificationCategories" o
                WHERE o."Level" = 3 AND o."ParentId" IS NULL
                  AND o."Id" <> (SELECT MIN(k."Id") FROM "FileClassificationCategories" k
                                 WHERE k."Level" = 3 AND k."ParentId" IS NULL AND k."Name" = o."Name" COLLATE NOCASE);

                INSERT OR IGNORE INTO "FileClassifications" ("FileDetailId", "CategoryId")
                SELECT fc."FileDetailId", m.KeeperId
                FROM "FileClassifications" fc
                JOIN _orphan_self_dups m ON m.DupId = fc."CategoryId";

                DELETE FROM "FileClassifications" WHERE "CategoryId" IN (SELECT DupId FROM _orphan_self_dups);

                INSERT OR IGNORE INTO "FileClassificationKeywords" ("Keyword", "CategoryId")
                SELECT k."Keyword", m.KeeperId
                FROM "FileClassificationKeywords" k
                JOIN _orphan_self_dups m ON m.DupId = k."CategoryId";

                DELETE FROM "FileClassificationKeywords" WHERE "CategoryId" IN (SELECT DupId FROM _orphan_self_dups);

                DELETE FROM "FileClassificationCategories" WHERE "Id" IN (SELECT DupId FROM _orphan_self_dups);

                INSERT INTO "FileClassificationCategories" ("Name", "Level", "IsFamous", "IsInternet", "IncludeInSearch")
                SELECT 'Unclassified', 1, 0, 0, 0
                WHERE EXISTS (SELECT 1 FROM "FileClassificationCategories" WHERE "Level" = 3 AND "ParentId" IS NULL)
                  AND NOT EXISTS (SELECT 1 FROM "FileClassificationCategories" WHERE "Level" = 1 AND "Name" = 'Unclassified' COLLATE NOCASE);

                CREATE TEMP TABLE _root AS
                SELECT "Id" FROM "FileClassificationCategories"
                WHERE "Level" = 1 AND "Name" = 'Unclassified' COLLATE NOCASE
                ORDER BY "Id" LIMIT 1;

                CREATE TEMP TABLE _orphan_root_dups AS
                SELECT o."Id" AS DupId, k."Id" AS KeeperId
                FROM "FileClassificationCategories" o
                JOIN "FileClassificationCategories" k ON k."ParentId" = (SELECT "Id" FROM _root)
                   AND k."Id" <> o."Id"
                   AND k."Name" = o."Name" COLLATE NOCASE
                WHERE o."Level" = 3 AND o."ParentId" IS NULL;

                INSERT OR IGNORE INTO "FileClassifications" ("FileDetailId", "CategoryId")
                SELECT fc."FileDetailId", m.KeeperId
                FROM "FileClassifications" fc
                JOIN _orphan_root_dups m ON m.DupId = fc."CategoryId";

                DELETE FROM "FileClassifications" WHERE "CategoryId" IN (SELECT DupId FROM _orphan_root_dups);

                INSERT OR IGNORE INTO "FileClassificationKeywords" ("Keyword", "CategoryId")
                SELECT k."Keyword", m.KeeperId
                FROM "FileClassificationKeywords" k
                JOIN _orphan_root_dups m ON m.DupId = k."CategoryId";

                DELETE FROM "FileClassificationKeywords" WHERE "CategoryId" IN (SELECT DupId FROM _orphan_root_dups);

                DELETE FROM "FileClassificationCategories" WHERE "Id" IN (SELECT DupId FROM _orphan_root_dups);

                UPDATE "FileClassificationCategories"
                SET "Level" = 2, "ParentId" = (SELECT "Id" FROM _root)
                WHERE "Level" = 3 AND "ParentId" IS NULL;

                DROP TABLE _dup_categories;
                DROP TABLE _twin_files;
                DROP TABLE _twin_owned;
                DROP TABLE _orphan_self_dups;
                DROP TABLE _root;
                DROP TABLE _orphan_root_dups;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
