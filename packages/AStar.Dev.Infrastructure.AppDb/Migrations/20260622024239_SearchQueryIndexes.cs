using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class SearchQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SyncedItems_AccountId_IsFolder_RemotePath",
                table: "SyncedItems",
                columns: ["AccountId", "IsFolder", "RemotePath"]);

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItems_AccountId_IsFolder_SizeInBytes",
                table: "SyncedItems",
                columns: ["AccountId", "IsFolder", "SizeInBytes"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncedItems_AccountId_IsFolder_RemotePath",
                table: "SyncedItems");

            migrationBuilder.DropIndex(
                name: "IX_SyncedItems_AccountId_IsFolder_SizeInBytes",
                table: "SyncedItems");
        }
    }
}
