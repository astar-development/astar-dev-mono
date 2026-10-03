using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefunctFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionCookie",
                table: "UserConfigurations");

            migrationBuilder.DropColumn(
                name: "BaseDirectory",
                table: "ScrapeDirectories");

            migrationBuilder.DropColumn(
                name: "BaseDirectoryFamous",
                table: "ScrapeDirectories");

            migrationBuilder.RenameColumn(
                name: "BaseSaveDirectory",
                table: "ScrapeDirectories",
                newName: "RootDirectoryFamous");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RootDirectoryFamous",
                table: "ScrapeDirectories",
                newName: "BaseSaveDirectory");

            migrationBuilder.AddColumn<string>(
                name: "SessionCookie",
                table: "UserConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "BaseDirectory",
                table: "ScrapeDirectories",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "BaseDirectoryFamous",
                table: "ScrapeDirectories",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");
        }
    }
}
