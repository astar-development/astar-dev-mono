using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapeConfigurationNoMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AlterColumn<string>(
                name: "SearchConfiguration_SearchCategories",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)");

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.AlterColumn<string>(
                name: "SearchConfiguration_SearchCategories",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)");
    }
}
