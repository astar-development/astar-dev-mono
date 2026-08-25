using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations;

/// <inheritdoc />
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class AddSearchConfiguration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SearchConfigurations_ScrapeConfigurations_ScrapeConfigurationEntityId",
            table: "SearchConfigurations");

        migrationBuilder.DropIndex(
            name: "IX_SearchConfigurations_ScrapeConfigurationEntityId",
            table: "SearchConfigurations");

        migrationBuilder.RenameColumn(
            name: "ScrapeConfigurationEntityId",
            table: "SearchConfigurations",
            newName: "UpdatedAt");

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

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "SearchConfigurations",
            type: "TEXT",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

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

        migrationBuilder.AddColumn<Guid>(
            name: "ScrapeConfigurationId",
            table: "SearchConfigurations",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

        migrationBuilder.CreateTable(
            name: "SearchCategoryEntity",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                SearchConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                LastKnownImageCount = table.Column<int>(type: "INTEGER", nullable: false),
                LastPageVisited = table.Column<int>(type: "INTEGER", nullable: false),
                TotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                IsFamous = table.Column<bool>(type: "INTEGER", nullable: false),
                IsInternet = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SearchCategoryEntity", x => x.Id);
                table.ForeignKey(
                    name: "FK_SearchCategoryEntity_SearchConfigurations_SearchConfigurationId",
                    column: x => x.SearchConfigurationId,
                    principalTable: "SearchConfigurations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SearchConfigurations_ScrapeConfigurationId",
            table: "SearchConfigurations",
            column: "ScrapeConfigurationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SearchCategoryEntity_SearchConfigurationId",
            table: "SearchCategoryEntity",
            column: "SearchConfigurationId");

        migrationBuilder.AddForeignKey(
            name: "FK_SearchConfigurations_ScrapeConfigurations_ScrapeConfigurationId",
            table: "SearchConfigurations",
            column: "ScrapeConfigurationId",
            principalTable: "ScrapeConfigurations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SearchConfigurations_ScrapeConfigurations_ScrapeConfigurationId",
            table: "SearchConfigurations");

        migrationBuilder.DropTable(
            name: "SearchCategoryEntity");

        migrationBuilder.DropIndex(
            name: "IX_SearchConfigurations_ScrapeConfigurationId",
            table: "SearchConfigurations");

        migrationBuilder.DropColumn(
            name: "ApiKey",
            table: "SearchConfigurations");

        migrationBuilder.DropColumn(
            name: "BaseUrl",
            table: "SearchConfigurations");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "SearchConfigurations");

        migrationBuilder.DropColumn(
            name: "ImagePauseInSeconds",
            table: "SearchConfigurations");

        migrationBuilder.DropColumn(
            name: "LoginUrl",
            table: "SearchConfigurations");

        migrationBuilder.DropColumn(
            name: "ScrapeConfigurationId",
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

        migrationBuilder.RenameColumn(
            name: "UpdatedAt",
            table: "SearchConfigurations",
            newName: "ScrapeConfigurationEntityId");

        migrationBuilder.CreateIndex(
            name: "IX_SearchConfigurations_ScrapeConfigurationEntityId",
            table: "SearchConfigurations",
            column: "ScrapeConfigurationEntityId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_SearchConfigurations_ScrapeConfigurations_ScrapeConfigurationEntityId",
            table: "SearchConfigurations",
            column: "ScrapeConfigurationEntityId",
            principalTable: "ScrapeConfigurations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
