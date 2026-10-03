using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScrapeLevelPropertiesFromSearchConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "ImagePauseInSeconds",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "LoginUrl",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "SearchString",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "SearchStringPrefix",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "SearchStringSuffix",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "SlowMotionDelay",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "StartingPageNumber",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "Subscriptions",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "SubscriptionsStartingPageNumber",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "SubscriptionsTotalPages",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "TopWallpapers",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "TopWallpapersStartingPageNumber",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "TopWallpapersTotalPages",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "TotalPages",
                table: "SearchConfigurations");

            migrationBuilder.DropColumn(
                name: "UseHeadless",
                table: "SearchConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ImagePauseInSeconds",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LoginUrl",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchString",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "SearchStringPrefix",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "SearchStringSuffix",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<float>(
                name: "SlowMotionDelay",
                table: "SearchConfigurations",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartingPageNumber",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Subscriptions",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionsStartingPageNumber",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionsTotalPages",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TopWallpapers",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<int>(
                name: "TopWallpapersStartingPageNumber",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TopWallpapersTotalPages",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPages",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseHeadless",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
