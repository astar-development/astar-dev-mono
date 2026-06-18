using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class DropFileClassificationRulesTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
INSERT OR IGNORE INTO FileClassificationCategories (Name, Level, ParentId)
SELECT DISTINCT Level1, 1, NULL
FROM FileClassificationRules
WHERE Level1 IS NOT NULL AND Level1 != '';");

        migrationBuilder.Sql(@"
INSERT OR IGNORE INTO FileClassificationCategories (Name, Level, ParentId)
SELECT DISTINCT r.Level2, 2, c1.Id
FROM FileClassificationRules r
JOIN FileClassificationCategories c1 ON c1.Name = r.Level1 AND c1.Level = 1
WHERE r.Level2 IS NOT NULL AND r.Level2 != '';");

        migrationBuilder.Sql(@"
INSERT OR IGNORE INTO FileClassificationCategories (Name, Level, ParentId)
SELECT DISTINCT r.Level3, 3, c2.Id
FROM FileClassificationRules r
JOIN FileClassificationCategories c1 ON c1.Name = r.Level1 AND c1.Level = 1
JOIN FileClassificationCategories c2 ON c2.Name = r.Level2 AND c2.Level = 2 AND c2.ParentId = c1.Id
WHERE r.Level3 IS NOT NULL AND r.Level3 != '';");

        migrationBuilder.Sql(@"
WITH RECURSIVE split(level1, level2, level3, kw, rest) AS (
    SELECT Level1, Level2, Level3,
        CASE WHEN instr(Keywords, '|') > 0 THEN substr(Keywords, 1, instr(Keywords, '|') - 1) ELSE Keywords END,
        CASE WHEN instr(Keywords, '|') > 0 THEN substr(Keywords, instr(Keywords, '|') + 1) ELSE '' END
    FROM FileClassificationRules
    WHERE Keywords IS NOT NULL AND Keywords != ''
    UNION ALL
    SELECT level1, level2, level3,
        CASE WHEN instr(rest, '|') > 0 THEN substr(rest, 1, instr(rest, '|') - 1) ELSE rest END,
        CASE WHEN instr(rest, '|') > 0 THEN substr(rest, instr(rest, '|') + 1) ELSE '' END
    FROM split
    WHERE rest IS NOT NULL AND rest != ''
),
categorised AS (
    SELECT DISTINCT trim(s.kw) AS kw,
        COALESCE(
            (SELECT c3.Id FROM FileClassificationCategories c3
             JOIN FileClassificationCategories c2p ON c3.ParentId = c2p.Id
             JOIN FileClassificationCategories c1p ON c2p.ParentId = c1p.Id
             WHERE c3.Level = 3 AND c3.Name = s.level3 AND c2p.Name = s.level2 AND c1p.Name = s.level1
             LIMIT 1),
            (SELECT c2.Id FROM FileClassificationCategories c2
             JOIN FileClassificationCategories c1p ON c2.ParentId = c1p.Id
             WHERE c2.Level = 2 AND c2.Name = s.level2 AND c1p.Name = s.level1
             LIMIT 1),
            (SELECT c1.Id FROM FileClassificationCategories c1
             WHERE c1.Level = 1 AND c1.Name = s.level1
             LIMIT 1)
        ) AS category_id
    FROM split s
    WHERE trim(s.kw) IS NOT NULL AND trim(s.kw) != ''
)
INSERT INTO FileClassificationKeywords (Keyword, CategoryId)
SELECT kw, category_id
FROM categorised
WHERE category_id IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM FileClassificationKeywords fck
    WHERE fck.Keyword = categorised.kw AND fck.CategoryId = categorised.category_id
);");

        migrationBuilder.Sql(@"
INSERT OR IGNORE INTO FileClassificationCategories (Name, Level, ParentId)
VALUES
    ('Person', 1, NULL),
    ('Colour',  1, NULL),
    ('Object', 1, NULL),
    ('Place',  1, NULL),
    ('Event',  1, NULL),
    ('Red',  2, 2),
    ('Yellow',  2, 2),
    ('Green',  2, 2),
    ('Blue',  2, 2),
    ('Orange',  2, 2),
    ('Pink',  2, 2),
    ('Black',  2, 2),
    ('Purple',  2, 2);");

        migrationBuilder.Sql(@"
INSERT OR IGNORE INTO FileClassificationKeywords (Keyword, CategoryId)
VALUES
    ('Person', 1),
    ('Colour',  2),
    ('Object', 3),
    ('Place',  4),
    ('Event',  5);");

        migrationBuilder.DropTable(name: "FileClassificationRules");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("DropFileClassificationRulesTable migration cannot be reversed.");
}
