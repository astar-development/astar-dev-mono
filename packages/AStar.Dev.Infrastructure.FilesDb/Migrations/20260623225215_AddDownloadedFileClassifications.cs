using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadedFileClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadedFileClassification",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileDetailId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileClassificationId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadedFileClassification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadedFileClassification_FileClassification_FileClassificationId",
                        column: x => x.FileClassificationId,
                        principalSchema: "files",
                        principalTable: "FileClassification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DownloadedFileClassification_FileDetail_FileDetailId",
                        column: x => x.FileDetailId,
                        principalSchema: "files",
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadedFileClassification_FileClassificationId",
                schema: "files",
                table: "DownloadedFileClassification",
                column: "FileClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadedFileClassification_FileDetailId_FileClassificationId",
                schema: "files",
                table: "DownloadedFileClassification",
                columns: new[] { "FileDetailId", "FileClassificationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
                name: "DownloadedFileClassification",
                schema: "files");
    }
}
