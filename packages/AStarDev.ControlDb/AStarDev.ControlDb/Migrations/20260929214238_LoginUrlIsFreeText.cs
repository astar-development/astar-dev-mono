using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class LoginUrlIsFreeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LoginUrl",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LoginUrl",
                table: "ScrapeConfigurations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");
        }
    }
}
