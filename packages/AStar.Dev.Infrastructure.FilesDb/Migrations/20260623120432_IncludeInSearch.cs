using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class IncludeInSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<bool>(
                name: "IncludeInSearch",
                schema: "files",
                table: "ScrapedTag",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
                name: "IncludeInSearch",
                schema: "files",
                table: "ScrapedTag");
    }
}
