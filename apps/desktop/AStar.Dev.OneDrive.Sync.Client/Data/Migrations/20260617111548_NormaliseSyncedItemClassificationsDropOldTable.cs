using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class NormaliseSyncedItemClassificationsDropOldTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
            name: "SyncedItemClassifications");

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SyncedItemClassifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                SyncedItemId = table.Column<int>(type: "INTEGER", nullable: false),
                IsSpecial = table.Column<bool>(type: "INTEGER", nullable: false),
                Level1 = table.Column<string>(type: "TEXT", nullable: false),
                Level2 = table.Column<string>(type: "TEXT", nullable: true),
                Level3 = table.Column<string>(type: "TEXT", nullable: true),
                TagName = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SyncedItemClassifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_SyncedItemClassifications_SyncedItems_SyncedItemId",
                    column: x => x.SyncedItemId,
                    principalTable: "SyncedItems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

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
            name: "IX_SyncedItemClassifications_SyncedItemId",
            table: "SyncedItemClassifications",
            column: "SyncedItemId");
    }
}
