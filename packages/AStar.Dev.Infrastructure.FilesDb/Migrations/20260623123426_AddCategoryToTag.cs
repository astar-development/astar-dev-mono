using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
                name: "Category",
                schema: "files",
                table: "ScrapedTag",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
                name: "Category",
                schema: "files",
                table: "ScrapedTag");
    }
}
