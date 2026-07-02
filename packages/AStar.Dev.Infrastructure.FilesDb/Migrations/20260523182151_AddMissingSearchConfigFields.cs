using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSearchConfigFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginUrl",
                schema: "files",
                table: "SearchConfiguration",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "SlowMotionDelay",
                schema: "files",
                table: "SearchConfiguration",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseHeadless",
                schema: "files",
                table: "SearchConfiguration",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginUrl",
                schema: "files",
                table: "SearchConfiguration");

            migrationBuilder.DropColumn(
                name: "SlowMotionDelay",
                schema: "files",
                table: "SearchConfiguration");

            migrationBuilder.DropColumn(
                name: "UseHeadless",
                schema: "files",
                table: "SearchConfiguration");
        }
    }
}
