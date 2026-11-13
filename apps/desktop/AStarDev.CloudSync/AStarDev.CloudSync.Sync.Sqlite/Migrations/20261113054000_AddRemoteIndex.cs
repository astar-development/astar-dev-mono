using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Sync.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteCursors",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cursor = table.Column<string>(type: "TEXT", nullable: true),
                    ResyncPass = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteCursors", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "RemoteItems",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RemoteItemId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    ETag = table.Column<string>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSeenPass = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteItems", x => new { x.AccountId, x.RemoteItemId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemoteItems_AccountId_ParentId",
                table: "RemoteItems",
                columns: new[] { "AccountId", "ParentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteCursors");

            migrationBuilder.DropTable(
                name: "RemoteItems");
        }
    }
}
