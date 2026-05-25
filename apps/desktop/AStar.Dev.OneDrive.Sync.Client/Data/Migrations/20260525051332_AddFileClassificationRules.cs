using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileClassificationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileClassificationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Keywords = table.Column<string>(type: "TEXT", nullable: false),
                    Level1 = table.Column<string>(type: "TEXT", nullable: false),
                    Level2 = table.Column<string>(type: "TEXT", nullable: true),
                    Level3 = table.Column<string>(type: "TEXT", nullable: true),
                    IsSpecial = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileClassificationRules", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileClassificationRules");
        }
    }
}
