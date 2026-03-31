using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocaleToAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "en-GB");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Locale",
                table: "AppSettings");
        }
    }
}
