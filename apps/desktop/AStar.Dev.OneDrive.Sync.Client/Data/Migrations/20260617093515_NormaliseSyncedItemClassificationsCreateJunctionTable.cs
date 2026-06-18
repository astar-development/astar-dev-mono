using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class NormaliseSyncedItemClassificationsCreateJunctionTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SyncedItemFileClassifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SyncedItemId = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncedItemFileClassifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncedItemFileClassifications_FileClassificationCategories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "FileClassificationCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
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
            name: "IX_SyncedItemFileClassifications_SyncedItemId_CategoryId",
            table: "SyncedItemFileClassifications",
            columns: ["SyncedItemId", "CategoryId"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SyncedItemFileClassifications");
    }
}
