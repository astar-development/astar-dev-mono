using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations;

/// <inheritdoc />
public partial class Indexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_LocalPath",
            table: "SyncedItems",
            column: "LocalPath");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_RemotePath",
            table: "SyncedItems",
            column: "RemotePath");

        migrationBuilder.CreateIndex(
            name: "IX_SyncedItems_SizeInBytes",
            table: "SyncedItems",
            column: "SizeInBytes");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SyncedItems_LocalPath",
            table: "SyncedItems");

        migrationBuilder.DropIndex(
            name: "IX_SyncedItems_RemotePath",
            table: "SyncedItems");

        migrationBuilder.DropIndex(
            name: "IX_SyncedItems_SizeInBytes",
            table: "SyncedItems");
    }
}
