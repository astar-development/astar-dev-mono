using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddTagIsFamous : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFamous",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Seed the flag for tags stored before it existed, with the same rule FamousTagCheck uses for new tags: an upper-case ASCII first letter and a person category.
            migrationBuilder.Sql(
                "UPDATE Tags SET IsFamous = 1 WHERE substr(Name, 1, 1) GLOB '[A-Z]' AND lower(Category) IN (SELECT lower(Name) FROM PersonCategoryEntity);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFamous",
                table: "Tags");
        }
    }
}
