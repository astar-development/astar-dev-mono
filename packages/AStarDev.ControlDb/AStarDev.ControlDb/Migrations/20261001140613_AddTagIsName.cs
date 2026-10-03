using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddTagIsName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsName",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsName",
                table: "Tags");
        }
    }
}
