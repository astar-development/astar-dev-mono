using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
                name: "ScrapeConfiguration",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConnectionStrings_Sqlite = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ScrapeDirectories_BaseDirectory = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ScrapeDirectories_BaseDirectoryFamous = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ScrapeDirectories_BaseSaveDirectory = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ScrapeDirectories_RootDirectory = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ScrapeDirectories_SubDirectoryName = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_ApiKey = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_BaseUrl = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_ImagePauseInSeconds = table.Column<int>(type: "int", nullable: false),
                    SearchConfiguration_SearchCategories = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_SearchString = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_SearchStringPrefix = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_SearchStringSuffix = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_StartingPageNumber = table.Column<int>(type: "int", nullable: false),
                    SearchConfiguration_Subscriptions = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_SubscriptionsStartingPageNumber = table.Column<int>(type: "int", nullable: false),
                    SearchConfiguration_SubscriptionsTotalPages = table.Column<int>(type: "int", nullable: false),
                    SearchConfiguration_TopWallpapers = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchConfiguration_TopWallpapersStartingPageNumber = table.Column<int>(type: "int", nullable: false),
                    SearchConfiguration_TopWallpapersTotalPages = table.Column<int>(type: "int", nullable: false),
                    SearchConfiguration_TotalPages = table.Column<int>(type: "int", nullable: false),
                    UserConfiguration_LoginEmailAddress = table.Column<string>(type: "TEXT", nullable: false),
                    UserConfiguration_Password = table.Column<string>(type: "TEXT", nullable: false),
                    UserConfiguration_SessionCookie = table.Column<string>(type: "TEXT", nullable: false),
                    UserConfiguration_Username = table.Column<string>(type: "nvarchar(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeConfiguration", x => x.Id);
                });

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
                name: "ScrapeConfiguration",
                schema: "files");
    }
}
