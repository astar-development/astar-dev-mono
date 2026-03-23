using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Api.Usage.Logger.Migrations;

/// <inheritdoc />
public partial class AddHttpMethod : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AlterColumn<double>(
            name: "ElapsedMilliseconds",
            schema: "usage",
            table: "ApiUsageEvent",
            type: "float",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "bigint");

        _ = migrationBuilder.AddColumn<string>(
            name: "HttpMethod",
            schema: "usage",
            table: "ApiUsageEvent",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "HttpMethod",
            schema: "usage",
            table: "ApiUsageEvent");

        _ = migrationBuilder.AlterColumn<long>(
            name: "ElapsedMilliseconds",
            schema: "usage",
            table: "ApiUsageEvent",
            type: "bigint",
            nullable: false,
            oldClrType: typeof(double),
            oldType: "float");
    }
}
