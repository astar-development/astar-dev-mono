using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeInBytesToSyncedItems : Migration
    {
        private static readonly string[] accountIdSizeInBytesColumns = ["AccountId", "SizeInBytes"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SizeInBytes",
                table: "SyncedItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncedItems_AccountId_SizeInBytes",
                table: "SyncedItems",
                columns: accountIdSizeInBytesColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncedItems_AccountId_SizeInBytes",
                table: "SyncedItems");

            migrationBuilder.DropColumn(
                name: "SizeInBytes",
                table: "SyncedItems");
        }
    }
}
