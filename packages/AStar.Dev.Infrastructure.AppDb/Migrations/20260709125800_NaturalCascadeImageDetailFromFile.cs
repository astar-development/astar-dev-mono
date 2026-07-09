using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class NaturalCascadeImageDetailFromFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileDetail_ImageDetail_ImageDetailId",
                table: "FileDetail");

            migrationBuilder.DropIndex(
                name: "IX_FileDetail_ImageDetailId",
                table: "FileDetail");

            migrationBuilder.AddColumn<Guid>(
                name: "FileDetailId",
                table: "ImageDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ImageDetail"
                SET "FileDetailId" = (
                    SELECT "Id"
                    FROM "FileDetail"
                    WHERE "FileDetail"."ImageDetailId" = "ImageDetail"."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "FileDetail"
                    WHERE "FileDetail"."ImageDetailId" = "ImageDetail"."Id"
                );
                """);

            migrationBuilder.Sql("DELETE FROM \"ImageDetail\" WHERE \"FileDetailId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileDetailId",
                table: "ImageDetail",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ImageDetailId",
                table: "FileDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ImageDetail_FileDetailId",
                table: "ImageDetail",
                column: "FileDetailId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageDetail_FileDetail_FileDetailId",
                table: "ImageDetail",
                column: "FileDetailId",
                principalTable: "FileDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageDetail_FileDetail_FileDetailId",
                table: "ImageDetail");

            migrationBuilder.DropIndex(
                name: "IX_ImageDetail_FileDetailId",
                table: "ImageDetail");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageDetailId",
                table: "FileDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "FileDetail"
                SET "ImageDetailId" = (
                    SELECT "Id"
                    FROM "ImageDetail"
                    WHERE "ImageDetail"."FileDetailId" = "FileDetail"."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "ImageDetail"
                    WHERE "ImageDetail"."FileDetailId" = "FileDetail"."Id"
                );
                """);

            migrationBuilder.Sql("DELETE FROM \"FileDetail\" WHERE \"ImageDetailId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "ImageDetailId",
                table: "FileDetail",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_ImageDetailId",
                table: "FileDetail",
                column: "ImageDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileDetail_ImageDetail_ImageDetailId",
                table: "FileDetail",
                column: "ImageDetailId",
                principalTable: "ImageDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "FileDetailId",
                table: "ImageDetail");
        }
    }
}
