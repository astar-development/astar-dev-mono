using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class AddDatesToFileClassificationCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Height",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "FileDetail");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "FileClassificationCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FileClassificationCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "FileClassificationCategories",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FileClassificationCategories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FileClassificationCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FileClassificationCategories");

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
