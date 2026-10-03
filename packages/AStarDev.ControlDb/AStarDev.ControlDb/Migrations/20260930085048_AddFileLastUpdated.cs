using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddFileLastUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastUpdated_Ticks",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            // Existing rows: the closest record of when a file was recorded is its access detail's DetailsLastUpdated.
            migrationBuilder.Sql(
                "UPDATE FileDetail SET LastUpdated_Ticks = COALESCE((SELECT DetailsLastUpdated_Ticks FROM FileAccessDetail WHERE FileAccessDetail.FileId = FileDetail.Id), 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdated_Ticks",
                table: "FileDetail");
        }
    }
}
