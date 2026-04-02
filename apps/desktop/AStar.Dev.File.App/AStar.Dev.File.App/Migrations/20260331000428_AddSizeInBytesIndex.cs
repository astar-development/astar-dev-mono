using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.File.App.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeInBytesIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ScannedFiles_SizeInBytes",
                table: "ScannedFiles",
                column: "SizeInBytes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScannedFiles_SizeInBytes",
                table: "ScannedFiles");
        }
    }
}
