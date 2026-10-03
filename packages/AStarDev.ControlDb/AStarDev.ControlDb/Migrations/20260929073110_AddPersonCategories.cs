using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonCategoryEntity",
                columns: table => new
                {
                    Id = table.Column<byte[]>(type: "BLOB", nullable: false),
                    SearchConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonCategoryEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonCategoryEntity_SearchConfigurations_SearchConfigurationId",
                        column: x => x.SearchConfigurationId,
                        principalTable: "SearchConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonCategoryEntity_SearchConfigurationId",
                table: "PersonCategoryEntity",
                column: "SearchConfigurationId");

            foreach (var name in new[] { "Celebrities", "Models", "Pornstars", "Other Figures", "Actress" })
            {
                migrationBuilder.Sql($@"INSERT INTO ""PersonCategoryEntity"" (""Id"", ""SearchConfigurationId"", ""Name"", ""CreatedAt_Ticks"", ""UpdatedAt_Ticks"")
SELECT randomblob(16), ""Id"", '{name}', 0, 0 FROM ""SearchConfigurations"";");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonCategoryEntity");
        }
    }
}
