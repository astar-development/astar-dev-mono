using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncStateRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncStateRecords",
                columns: table => new
                {
                    AccountId      = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    State          = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CheckpointJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStateRecords", x => x.AccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SyncStateRecords");
        }
    }
}
