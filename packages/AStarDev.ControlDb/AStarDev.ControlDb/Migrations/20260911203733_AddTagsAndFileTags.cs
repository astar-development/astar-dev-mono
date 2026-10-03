using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddTagsAndFileTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WallhavenTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Alias = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Purity = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileTags",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTags", x => new { x.FileId, x.TagId });
                    table.ForeignKey(
                        name: "FK_FileTags_FileDetail_FileId",
                        column: x => x.FileId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileTags_TagId",
                table: "FileTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_WallhavenTagId",
                table: "Tags",
                column: "WallhavenTagId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileTags");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
