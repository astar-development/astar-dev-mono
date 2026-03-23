using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Api.Usage.Logger.Migrations;

/// <inheritdoc />
public partial class UseDateTime : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AlterColumn<DateTime>(
            name: "Timestamp",
            schema: "usage",
            table: "ApiUsageEvent",
            type: "datetime2",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "DateTime");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AlterColumn<DateTime>(
            name: "Timestamp",
            schema: "usage",
            table: "ApiUsageEvent",
            type: "DateTime",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");
    }
}
