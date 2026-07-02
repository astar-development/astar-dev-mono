using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapeConfigurationMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectionStrings_Sqlite",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeDirectories_BaseDirectory",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeDirectories_BaseDirectoryFamous",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeDirectories_BaseSaveDirectory",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeDirectories_RootDirectory",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "ScrapeDirectories_SubDirectoryName",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_ApiKey",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_BaseUrl",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_ImagePauseInSeconds",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_SearchCategories",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_SearchString",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_SearchStringPrefix",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_SearchStringSuffix",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_StartingPageNumber",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_Subscriptions",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_SubscriptionsStartingPageNumber",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_SubscriptionsTotalPages",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_TopWallpapers",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_TopWallpapersStartingPageNumber",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_TopWallpapersTotalPages",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "SearchConfiguration_TotalPages",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "UserConfiguration_LoginEmailAddress",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "UserConfiguration_Password",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "UserConfiguration_SessionCookie",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.DropColumn(
                name: "UserConfiguration_Username",
                schema: "files",
                table: "ScrapeConfiguration");

            migrationBuilder.CreateTable(
                name: "ConnectionStrings",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sqlite = table.Column<string>(type: "nvarchar(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionStrings_ScrapeConfiguration_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalSchema: "files",
                        principalTable: "ScrapeConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeDirectories",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    RootDirectory = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    BaseSaveDirectory = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    BaseDirectory = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    BaseDirectoryFamous = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SubDirectoryName = table.Column<string>(type: "nvarchar(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeDirectories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapeDirectories_ScrapeConfiguration_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalSchema: "files",
                        principalTable: "ScrapeConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchConfiguration",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchString = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    TopWallpapers = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchStringPrefix = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SearchStringSuffix = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    Subscriptions = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    ImagePauseInSeconds = table.Column<int>(type: "int", nullable: false),
                    StartingPageNumber = table.Column<int>(type: "int", nullable: false),
                    TotalPages = table.Column<int>(type: "int", nullable: false),
                    SubscriptionsStartingPageNumber = table.Column<int>(type: "int", nullable: false),
                    SubscriptionsTotalPages = table.Column<int>(type: "int", nullable: false),
                    TopWallpapersTotalPages = table.Column<int>(type: "int", nullable: false),
                    TopWallpapersStartingPageNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalSchema: "files",
                        principalTable: "ScrapeConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConfiguration",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScrapeConfigurationEntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoginEmailAddress = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    SessionCookie = table.Column<string>(type: "nvarchar(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConfiguration_ScrapeConfiguration_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalSchema: "files",
                        principalTable: "ScrapeConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchCategories",
                schema: "files",
                columns: table => new
                {
                    SearchConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(128)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    LastKnownImageCount = table.Column<int>(type: "int", nullable: false),
                    LastPageVisited = table.Column<int>(type: "int", nullable: false),
                    TotalPages = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchCategories", x => new { x.SearchConfigurationId, x.Id });
                    table.ForeignKey(
                        name: "FK_SearchCategories_SearchConfiguration_SearchConfigurationId",
                        column: x => x.SearchConfigurationId,
                        principalSchema: "files",
                        principalTable: "SearchConfiguration",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionStrings_ScrapeConfigurationEntityId",
                schema: "files",
                table: "ConnectionStrings",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
                schema: "files",
                table: "ScrapeDirectories",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchConfiguration_ScrapeConfigurationEntityId",
                schema: "files",
                table: "SearchConfiguration",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConfiguration_ScrapeConfigurationEntityId",
                schema: "files",
                table: "UserConfiguration",
                column: "ScrapeConfigurationEntityId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionStrings",
                schema: "files");

            migrationBuilder.DropTable(
                name: "ScrapeDirectories",
                schema: "files");

            migrationBuilder.DropTable(
                name: "SearchCategories",
                schema: "files");

            migrationBuilder.DropTable(
                name: "UserConfiguration",
                schema: "files");

            migrationBuilder.DropTable(
                name: "SearchConfiguration",
                schema: "files");

            migrationBuilder.AddColumn<string>(
                name: "ConnectionStrings_Sqlite",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScrapeDirectories_BaseDirectory",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScrapeDirectories_BaseDirectoryFamous",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScrapeDirectories_BaseSaveDirectory",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScrapeDirectories_RootDirectory",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScrapeDirectories_SubDirectoryName",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_ApiKey",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_BaseUrl",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_ImagePauseInSeconds",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_SearchCategories",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_SearchString",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_SearchStringPrefix",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_SearchStringSuffix",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_StartingPageNumber",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_Subscriptions",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_SubscriptionsStartingPageNumber",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_SubscriptionsTotalPages",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SearchConfiguration_TopWallpapers",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_TopWallpapersStartingPageNumber",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_TopWallpapersTotalPages",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SearchConfiguration_TotalPages",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserConfiguration_LoginEmailAddress",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserConfiguration_Password",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserConfiguration_SessionCookie",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserConfiguration_Username",
                schema: "files",
                table: "ScrapeConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");
        }
    }
}
