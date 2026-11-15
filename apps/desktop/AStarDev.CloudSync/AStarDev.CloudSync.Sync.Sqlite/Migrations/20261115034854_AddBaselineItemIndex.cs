using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddBaselineItemIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SyncBaseline_AccountId_RemoteItemId",
                table: "SyncBaseline",
                columns: new[] { "AccountId", "RemoteItemId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncBaseline_AccountId_RemoteItemId",
                table: "SyncBaseline");
        }
    }
}
