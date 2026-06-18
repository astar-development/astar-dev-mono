using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class CategoryChanges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_FileClassificationCategories_FileClassificationCategories_ParentId",
            table: "FileClassificationCategories");

        migrationBuilder.AddColumn<bool>(
            name: "IsFamous",
            table: "FileClassificationCategories",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsInternet",
            table: "FileClassificationCategories",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddForeignKey(
            name: "FK_FileClassificationCategories_FileClassificationCategories_ParentId",
            table: "FileClassificationCategories",
            column: "ParentId",
            principalTable: "FileClassificationCategories",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_FileClassificationCategories_FileClassificationCategories_ParentId",
            table: "FileClassificationCategories");

        migrationBuilder.DropColumn(
            name: "IsFamous",
            table: "FileClassificationCategories");

        migrationBuilder.DropColumn(
            name: "IsInternet",
            table: "FileClassificationCategories");

        migrationBuilder.AddForeignKey(
            name: "FK_FileClassificationCategories_FileClassificationCategories_ParentId",
            table: "FileClassificationCategories",
            column: "ParentId",
            principalTable: "FileClassificationCategories",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
