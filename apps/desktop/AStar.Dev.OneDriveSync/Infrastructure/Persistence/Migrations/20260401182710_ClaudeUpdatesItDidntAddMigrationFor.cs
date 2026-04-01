using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClaudeUpdatesItDidntAddMigrationFor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AuthState",
                table: "Accounts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "ConsentDecisionMadeAt",
                table: "Accounts",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserType",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AuthState",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ConsentDecisionMadeAt",
                table: "Accounts");
        }
    }
}
