using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations
{
    /// <inheritdoc />
    public partial class HierarchicalFileClassificationRules : Migration
    {
        private static readonly string[] CategoryIndexColumns = ["ParentId", "Name"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1 — Create FileClassificationCategories table
            migrationBuilder.CreateTable(
                name: "FileClassificationCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileClassificationCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileClassificationCategories_FileClassificationCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FileClassificationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Step 2 — Create FileClassificationKeywords table
            migrationBuilder.CreateTable(
                name: "FileClassificationKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Keyword = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_FileClassificationCategories_ParentId_Name",
                table: "FileClassificationCategories",
                columns: CategoryIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileClassificationKeywords_CategoryId",
                table: "FileClassificationKeywords",
                column: "CategoryId");

            // Step 3 — Seed L1 categories from old flat rules
            migrationBuilder.Sql(@"
INSERT INTO FileClassificationCategories (Name, Level, ParentId)
SELECT DISTINCT Level1, 1, NULL
FROM FileClassificationRules
WHERE Level1 IS NOT NULL AND Level1 != '';");

            // Step 4 — Seed L2 categories from old flat rules
            migrationBuilder.Sql(@"
INSERT INTO FileClassificationCategories (Name, Level, ParentId)
SELECT DISTINCT r.Level2, 2, c1.Id
FROM FileClassificationRules r
JOIN FileClassificationCategories c1 ON c1.Name = r.Level1 AND c1.Level = 1
WHERE r.Level2 IS NOT NULL AND r.Level2 != '';");

            // Step 5 — Seed L3 categories from old flat rules
            migrationBuilder.Sql(@"
INSERT INTO FileClassificationCategories (Name, Level, ParentId)
SELECT DISTINCT r.Level3, 3, c2.Id
FROM FileClassificationRules r
JOIN FileClassificationCategories c1 ON c1.Name = r.Level1 AND c1.Level = 1
JOIN FileClassificationCategories c2 ON c2.Name = r.Level2 AND c2.Level = 2 AND c2.ParentId = c1.Id
WHERE r.Level3 IS NOT NULL AND r.Level3 != '';");

            // Step 6 — Split pipe-delimited keywords in C# (via recursive CTE) and link to most-specific category
            migrationBuilder.Sql(@"
WITH RECURSIVE split(rule_id, level1, level2, level3, kw, rest) AS (
    SELECT
        Id,
        Level1,
        Level2,
        Level3,
        CASE WHEN instr(Keywords, '|') > 0
             THEN substr(Keywords, 1, instr(Keywords, '|') - 1)
             ELSE Keywords END,
        CASE WHEN instr(Keywords, '|') > 0
             THEN substr(Keywords, instr(Keywords, '|') + 1)
             ELSE '' END
    FROM FileClassificationRules
    WHERE Keywords IS NOT NULL AND Keywords != ''
    UNION ALL
    SELECT
        rule_id, level1, level2, level3,
        CASE WHEN instr(rest, '|') > 0
             THEN substr(rest, 1, instr(rest, '|') - 1)
             ELSE rest END,
        CASE WHEN instr(rest, '|') > 0
             THEN substr(rest, instr(rest, '|') + 1)
             ELSE '' END
    FROM split
    WHERE rest IS NOT NULL AND rest != ''
)
INSERT INTO FileClassificationKeywords (Keyword, CategoryId)
SELECT DISTINCT
    trim(s.kw),
    COALESCE(
        (SELECT c3.Id
         FROM FileClassificationCategories c3
         JOIN FileClassificationCategories c2p ON c3.ParentId = c2p.Id
         JOIN FileClassificationCategories c1p ON c2p.ParentId = c1p.Id
         WHERE c3.Level = 3 AND c3.Name = s.level3
           AND c2p.Name = s.level2 AND c1p.Name = s.level1
         LIMIT 1),
        (SELECT c2.Id
         FROM FileClassificationCategories c2
         JOIN FileClassificationCategories c1p ON c2.ParentId = c1p.Id
         WHERE c2.Level = 2 AND c2.Name = s.level2 AND c1p.Name = s.level1
         LIMIT 1),
        (SELECT c1.Id
         FROM FileClassificationCategories c1
         WHERE c1.Level = 1 AND c1.Name = s.level1
         LIMIT 1)
    ) AS CategoryId
FROM split s
WHERE trim(s.kw) IS NOT NULL AND trim(s.kw) != '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new System.NotSupportedException("HierarchicalFileClassificationRules migration cannot be reversed. Old FileClassificationRules table is still present.");
    }
}
