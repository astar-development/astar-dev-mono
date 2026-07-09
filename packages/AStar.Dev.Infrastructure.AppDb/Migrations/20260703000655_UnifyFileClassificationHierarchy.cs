using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class UnifyFileClassificationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileNamePart");

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInSearch",
                table: "FileClassificationCategories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DownloadedFileClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadedFileClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadedFileClassifications_FileClassificationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FileClassificationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DownloadedFileClassifications_FileDetail_FileDetailId",
                        column: x => x.FileDetailId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileClassificationKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Keyword = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFamous = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInternet = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileClassificationKeywords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileClassificationKeywords_FileClassificationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FileClassificationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadedFileClassifications_CategoryId",
                table: "DownloadedFileClassifications",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadedFileClassifications_FileDetailId_CategoryId",
                table: "DownloadedFileClassifications",
                columns: new[] { "FileDetailId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileClassificationKeywords_CategoryId_Keyword",
                table: "FileClassificationKeywords",
                columns: new[] { "CategoryId", "Keyword" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadedFileClassifications");

            migrationBuilder.DropTable(
                name: "FileClassificationKeywords");

            migrationBuilder.DropColumn(
                name: "IncludeInSearch",
                table: "FileClassificationCategories");

            migrationBuilder.CreateTable(
                name: "FileNamePart",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileNamePart", x => x.Id);
                });
        }
    }
}
