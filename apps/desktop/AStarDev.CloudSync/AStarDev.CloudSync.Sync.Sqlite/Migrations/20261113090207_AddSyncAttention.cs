using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncAttention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncAttention",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemKey = table.Column<string>(type: "TEXT", nullable: false),
                    IsConflict = table.Column<bool>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    ConflictKind = table.Column<string>(type: "TEXT", nullable: true),
                    LocalPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LocalModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    LocalTag = table.Column<string>(type: "TEXT", nullable: true),
                    RemotePresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RemoteSize = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteTag = table.Column<string>(type: "TEXT", nullable: true),
                    PairId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Direction = table.Column<string>(type: "TEXT", nullable: true),
                    DetectedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncAttention", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncAttentionPaths",
                columns: table => new
                {
                    AttentionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncAttentionPaths", x => new { x.AttentionId, x.Path });
                    table.ForeignKey(
                        name: "FK_SyncAttentionPaths_SyncAttention_AttentionId",
                        column: x => x.AttentionId,
                        principalTable: "SyncAttention",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncAttention_AccountId_ItemKey",
                table: "SyncAttention",
                columns: new[] { "AccountId", "ItemKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncAttentionPaths");

            migrationBuilder.DropTable(
                name: "SyncAttention");
        }
    }
}
