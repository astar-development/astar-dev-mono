using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Api.Usage.Logger.Migrations;

/// <inheritdoc />
public partial class ClusterOnTimestamp : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.CreateIndex(
            name: "UpdatedDate_IX",
            schema: "usage",
            table: "ApiUsageEvent",
            column: "Timestamp");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropIndex(
            name: "UpdatedDate_IX",
            schema: "usage",
            table: "ApiUsageEvent");
    }
}
