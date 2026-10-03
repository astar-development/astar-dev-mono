using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddHotWallpapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HotWallpapers",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "HotWallpapers",
                collation: "NOCASE");

            migrationBuilder.AddColumn<int>(
                name: "HotWallpapersStartingPageNumber",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HotWallpapersTotalPages",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HotWallpapers",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "HotWallpapersStartingPageNumber",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "HotWallpapersTotalPages",
                table: "ScrapeConfigurations");
        }
    }
}
