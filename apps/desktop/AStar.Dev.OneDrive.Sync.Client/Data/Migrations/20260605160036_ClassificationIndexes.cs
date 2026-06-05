using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class ClassificationIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationRules_IsSpecial",
            table: "FileClassificationRules",
            column: "IsSpecial");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationRules_Level1_Level2_Level3",
            table: "FileClassificationRules",
            columns: ["Level1", "Level2", "Level3"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FileClassificationRules_IsSpecial",
            table: "FileClassificationRules");

        migrationBuilder.DropIndex(
            name: "IX_FileClassificationRules_Level1_Level2_Level3",
            table: "FileClassificationRules");
    }
}
