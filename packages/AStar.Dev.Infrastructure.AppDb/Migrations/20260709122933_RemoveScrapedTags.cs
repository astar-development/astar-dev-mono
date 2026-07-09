using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScrapedTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapedTag");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "FileClassificationCategories",
                newName: "UpdatedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "FileClassificationCategories",
                newName: "CreatedAt_Ticks");

            migrationBuilder.AlterColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "FileClassificationCategories",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt_Ticks",
                table: "FileClassificationCategories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt_Ticks",
                table: "FileClassificationCategories",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt_Ticks",
                table: "FileClassificationCategories",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "FileClassificationCategories",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "FileClassificationCategories",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.CreateTable(
                name: "ScrapedTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedTag", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedTag_Value",
                table: "ScrapedTag",
                column: "Value",
                unique: true);
        }
    }
}
