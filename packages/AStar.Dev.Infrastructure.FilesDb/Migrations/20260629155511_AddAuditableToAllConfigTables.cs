using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableToAllConfigTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "UserConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "UserConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "TagToIgnore",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "TagToIgnore",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "SearchConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "SearchConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "SearchCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "SearchCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "ScrapedTag",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "ScrapedTag",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "ScrapeDirectories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "ScrapeDirectories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "ModelToIgnore",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "ModelToIgnore",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "DownloadedFileClassification",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "DownloadedFileClassification",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "files",
                table: "ConnectionStrings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "files",
                table: "ConnectionStrings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "UserConfiguration");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "UserConfiguration");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "TagToIgnore");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "TagToIgnore");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "SearchConfiguration");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "SearchConfiguration");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "SearchCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "SearchCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "ScrapedTag");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "ScrapedTag");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "ScrapeDirectories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "ScrapeDirectories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "ModelToIgnore");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "ModelToIgnore");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "DownloadedFileClassification");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "DownloadedFileClassification");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "files",
                table: "ConnectionStrings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "ConnectionStrings");
        }
    }
}
