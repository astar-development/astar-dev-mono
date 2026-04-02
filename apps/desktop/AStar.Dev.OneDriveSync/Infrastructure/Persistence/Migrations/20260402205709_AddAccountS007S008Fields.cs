using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDriveSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountS007S008Fields : Migration
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
                maxLength: 32,
                nullable: false,
                defaultValue: "Authenticated");

            migrationBuilder.AddColumn<int>(
                name: "ConcurrencyLimit",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<long>(
                name: "ConsentDecisionMadeAt",
                table: "Accounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSyncActive",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "LastSyncedAt",
                table: "Accounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalSyncPath",
                table: "Accounts",
                type: "TEXT",
                maxLength: 4096,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "StoreFileMetadata",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SyncIntervalMinutes",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 15);
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
                name: "ConcurrencyLimit",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ConsentDecisionMadeAt",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IsSyncActive",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LocalSyncPath",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "StoreFileMetadata",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "SyncIntervalMinutes",
                table: "Accounts");
        }
    }
}
