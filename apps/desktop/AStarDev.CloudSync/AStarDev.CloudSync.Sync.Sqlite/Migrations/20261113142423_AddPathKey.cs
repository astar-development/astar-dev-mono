using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPathKey : Migration
    {
        private static readonly string[] KeyedTables = ["SelectionRules", "SyncBaseline"];
        private static readonly string[] AllTables = ["SelectionRules", "SyncBaseline", "SyncRoots"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in AllTables)
            {
                migrationBuilder.AddColumn<string>(name: "PathKey", table: table, type: "TEXT", nullable: false, defaultValue: "");
                migrationBuilder.Sql($"UPDATE \"{table}\" SET \"PathKey\" = upper(\"Path\");");
            }

            migrationBuilder.DropIndex(name: "IX_SyncRoots_Path", table: "SyncRoots");
            foreach (var table in KeyedTables)
                migrationBuilder.DropPrimaryKey(name: $"PK_{table}", table: table);

            foreach (var table in AllTables)
                migrationBuilder.AlterColumn<string>(name: "Path", table: table, type: "TEXT", nullable: false, oldClrType: typeof(string), oldType: "TEXT", oldCollation: "NOCASE");

            foreach (var table in KeyedTables)
                migrationBuilder.AddPrimaryKey(name: $"PK_{table}", table: table, columns: ["AccountId", "PathKey"]);

            migrationBuilder.CreateIndex(name: "IX_SyncRoots_PathKey", table: "SyncRoots", column: "PathKey", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_SyncRoots_PathKey", table: "SyncRoots");
            foreach (var table in KeyedTables)
                migrationBuilder.DropPrimaryKey(name: $"PK_{table}", table: table);

            foreach (var table in AllTables)
            {
                migrationBuilder.DropColumn(name: "PathKey", table: table);
                migrationBuilder.AlterColumn<string>(name: "Path", table: table, type: "TEXT", nullable: false, collation: "NOCASE", oldClrType: typeof(string), oldType: "TEXT");
            }

            foreach (var table in KeyedTables)
                migrationBuilder.AddPrimaryKey(name: $"PK_{table}", table: table, columns: ["AccountId", "Path"]);

            migrationBuilder.CreateIndex(name: "IX_SyncRoots_Path", table: "SyncRoots", column: "Path", unique: true);
        }
    }
}
