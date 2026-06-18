using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class CategoryChanges2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "IsSpecial",
            table: "FileClassificationKeywords",
            newName: "IsInternet");

        migrationBuilder.AddColumn<bool>(
            name: "IsFamous",
            table: "FileClassificationKeywords",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsFamous",
            table: "FileClassificationKeywords");

        migrationBuilder.RenameColumn(
            name: "IsInternet",
            table: "FileClassificationKeywords",
            newName: "IsSpecial");
    }
}
