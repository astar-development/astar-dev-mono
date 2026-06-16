using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class Indexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SyncedItemClassifications_TagName",
            table: "SyncedItemClassifications");

        migrationBuilder.DropIndex(
            name: "IX_FileClassificationKeywords_CategoryId",
            table: "FileClassificationKeywords");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItemClassifications_Level1",
            table: "SyncedItemClassifications",
            column: "Level1");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItemClassifications_Level2",
            table: "SyncedItemClassifications",
            column: "Level2");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItemClassifications_Level3",
            table: "SyncedItemClassifications",
            column: "Level3");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationKeywords_CategoryId_Keyword",
            table: "FileClassificationKeywords",
            columns: ["CategoryId", "Keyword"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationKeywords_Keyword",
            table: "FileClassificationKeywords",
            column: "Keyword");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationCategories_Name",
            table: "FileClassificationCategories",
            column: "Name");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SyncedItemClassifications_Level1",
            table: "SyncedItemClassifications");

        migrationBuilder.DropIndex(
            name: "IX_SyncedItemClassifications_Level2",
            table: "SyncedItemClassifications");

        migrationBuilder.DropIndex(
            name: "IX_SyncedItemClassifications_Level3",
            table: "SyncedItemClassifications");

        migrationBuilder.DropIndex(
            name: "IX_FileClassificationKeywords_CategoryId_Keyword",
            table: "FileClassificationKeywords");

        migrationBuilder.DropIndex(
            name: "IX_FileClassificationKeywords_Keyword",
            table: "FileClassificationKeywords");

        migrationBuilder.DropIndex(
            name: "IX_FileClassificationCategories_Name",
            table: "FileClassificationCategories");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItemClassifications_TagName",
            table: "SyncedItemClassifications",
            column: "TagName");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationKeywords_CategoryId",
            table: "FileClassificationKeywords",
            column: "CategoryId");
    }
}
