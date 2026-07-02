using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToDateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "FileNamePart",
                newName: "UpdatedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "files",
                table: "FileNamePart",
                newName: "CreatedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "files",
                table: "FileClassification",
                newName: "UpdatedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "files",
                table: "FileClassification",
                newName: "CreatedAt_Ticks");

            migrationBuilder.AlterColumn<long>(
                name: "UpdatedAt_Ticks",
                schema: "files",
                table: "FileNamePart",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt_Ticks",
                schema: "files",
                table: "FileNamePart",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "UpdatedAt_Ticks",
                schema: "files",
                table: "FileClassification",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt_Ticks",
                schema: "files",
                table: "FileClassification",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt_Ticks",
                schema: "files",
                table: "FileNamePart",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt_Ticks",
                schema: "files",
                table: "FileNamePart",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt_Ticks",
                schema: "files",
                table: "FileClassification",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt_Ticks",
                schema: "files",
                table: "FileClassification",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "files",
                table: "FileNamePart",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "files",
                table: "FileNamePart",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "files",
                table: "FileClassification",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "files",
                table: "FileClassification",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
