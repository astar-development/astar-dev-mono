using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapeLevelPropertiesToScrapeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ImagePauseInSeconds",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LoginUrl",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchString",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "SearchStringPrefix",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "SearchStringSuffix",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<float>(
                name: "SlowMotionDelay",
                table: "ScrapeConfigurations",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartingPageNumber",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Subscriptions",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionsStartingPageNumber",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionsTotalPages",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TopWallpapers",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<int>(
                name: "TopWallpapersStartingPageNumber",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TopWallpapersTotalPages",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPages",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseHeadless",
                table: "ScrapeConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "ImagePauseInSeconds",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "LoginUrl",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "SearchString",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "SearchStringPrefix",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "SearchStringSuffix",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "SlowMotionDelay",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "StartingPageNumber",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "Subscriptions",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "SubscriptionsStartingPageNumber",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "SubscriptionsTotalPages",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "TopWallpapers",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "TopWallpapersStartingPageNumber",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "TopWallpapersTotalPages",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "TotalPages",
                table: "ScrapeConfigurations");

            migrationBuilder.DropColumn(
                name: "UseHeadless",
                table: "ScrapeConfigurations");
        }
    }
}
