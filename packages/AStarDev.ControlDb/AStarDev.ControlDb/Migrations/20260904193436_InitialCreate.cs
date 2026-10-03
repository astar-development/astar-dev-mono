using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScrapeConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScrapeDirectories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootDirectory = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    BaseSaveDirectory = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    BaseDirectory = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    BaseDirectoryFamous = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SubDirectoryName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeDirectories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapeDirectories_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SearchTerm = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    MaxResults = table.Column<int>(type: "INTEGER", nullable: true),
                    BaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SearchString = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    TopWallpapers = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SearchStringPrefix = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SearchStringSuffix = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Subscriptions = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    ImagePauseInSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    StartingPageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriptionsStartingPageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriptionsTotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                    TopWallpapersStartingPageNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TopWallpapersTotalPages = table.Column<int>(type: "INTEGER", nullable: false),
                    LoginUrl = table.Column<string>(type: "TEXT", nullable: false),
                    UseHeadless = table.Column<bool>(type: "INTEGER", nullable: false),
                    SlowMotionDelay = table.Column<float>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchConfigurations_ScrapeConfigurations_ScrapeConfigurationId",
                        column: x => x.ScrapeConfigurationId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScrapeConfigurationEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Username = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Password = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SessionCookie = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConfigurations_ScrapeConfigurations_ScrapeConfigurationEntityId",
                        column: x => x.ScrapeConfigurationEntityId,
                        principalTable: "ScrapeConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
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
                name: "IX_ScrapeDirectories_ScrapeConfigurationEntityId",
                table: "ScrapeDirectories",
                column: "ScrapeConfigurationEntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchCategoryEntity_SearchConfigurationId",
                table: "SearchCategoryEntity",
                column: "SearchConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchConfigurations_ScrapeConfigurationId",
                table: "SearchConfigurations",
                column: "ScrapeConfigurationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConfigurations_ScrapeConfigurationEntityId",
                table: "UserConfigurations",
                column: "ScrapeConfigurationEntityId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapeDirectories");

            migrationBuilder.DropTable(
                name: "SearchCategoryEntity");

            migrationBuilder.DropTable(
                name: "UserConfigurations");

            migrationBuilder.DropTable(
                name: "SearchConfigurations");

            migrationBuilder.DropTable(
                name: "ScrapeConfigurations");
        }
    }
}
