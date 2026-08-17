using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations;

/// <inheritdoc />
public partial class MigrateOfflineScraper : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsFamous",
            table: "SearchCategories",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsInternet",
            table: "SearchCategories",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<int>(
            name: "Priority",
            table: "FileClassificationCategories",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsFamous",
            table: "SearchCategories");

        migrationBuilder.DropColumn(
            name: "IsInternet",
            table: "SearchCategories");

        migrationBuilder.DropColumn(
            name: "Priority",
            table: "FileClassificationCategories");
    }
}
