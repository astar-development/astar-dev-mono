using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPathKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PathKey",
                table: "SyncRoots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PathKey",
                table: "SyncBaseline",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PathKey",
                table: "SelectionRules",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE \"SyncRoots\" SET \"PathKey\" = upper(\"Path\");");
            migrationBuilder.Sql("UPDATE \"SyncBaseline\" SET \"PathKey\" = upper(\"Path\");");
            migrationBuilder.Sql("UPDATE \"SelectionRules\" SET \"PathKey\" = upper(\"Path\");");

            migrationBuilder.DropIndex(
                name: "IX_SyncRoots_Path",
                table: "SyncRoots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncBaseline",
                table: "SyncBaseline");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SelectionRules",
                table: "SelectionRules");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "SyncRoots",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "SyncBaseline",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "SelectionRules",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldCollation: "NOCASE");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncBaseline",
                table: "SyncBaseline",
                columns: new[] { "AccountId", "PathKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_SelectionRules",
                table: "SelectionRules",
                columns: new[] { "AccountId", "PathKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRoots_PathKey",
                table: "SyncRoots",
                column: "PathKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncRoots_PathKey",
                table: "SyncRoots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncBaseline",
                table: "SyncBaseline");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SelectionRules",
                table: "SelectionRules");

            migrationBuilder.DropColumn(
                name: "PathKey",
                table: "SyncRoots");

            migrationBuilder.DropColumn(
                name: "PathKey",
                table: "SyncBaseline");

            migrationBuilder.DropColumn(
                name: "PathKey",
                table: "SelectionRules");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "SyncRoots",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "SyncBaseline",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "SelectionRules",
                type: "TEXT",
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncBaseline",
                table: "SyncBaseline",
                columns: new[] { "AccountId", "Path" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_SelectionRules",
                table: "SelectionRules",
                columns: new[] { "AccountId", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRoots_Path",
                table: "SyncRoots",
                column: "Path",
                unique: true);
        }
    }
}
