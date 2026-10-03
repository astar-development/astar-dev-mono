using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class ControlDbAddApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "UserConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "UserConfigurations");
    }
}
