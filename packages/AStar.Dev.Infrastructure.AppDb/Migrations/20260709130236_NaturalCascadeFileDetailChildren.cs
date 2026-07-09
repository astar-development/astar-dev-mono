using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations
{
    /// <inheritdoc />
    public partial class NaturalCascadeFileDetailChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileDetail_DeletionStatus_DeletionStatusId",
                table: "FileDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_FileDetail_FileAccessDetail_FileAccessDetailId",
                table: "FileDetail");

            migrationBuilder.DropIndex(
                name: "IX_FileDetail_DeletionStatusId",
                table: "FileDetail");

            migrationBuilder.DropIndex(
                name: "IX_FileDetail_FileAccessDetailId",
                table: "FileDetail");

            migrationBuilder.AddColumn<Guid>(
                name: "FileDetailId",
                table: "FileAccessDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FileDetailId",
                table: "DeletionStatus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "FileAccessDetail"
                SET "FileDetailId" = (
                    SELECT "Id"
                    FROM "FileDetail"
                    WHERE "FileDetail"."FileAccessDetailId" = "FileAccessDetail"."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "FileDetail"
                    WHERE "FileDetail"."FileAccessDetailId" = "FileAccessDetail"."Id"
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "DeletionStatus"
                SET "FileDetailId" = (
                    SELECT "Id"
                    FROM "FileDetail"
                    WHERE "FileDetail"."DeletionStatusId" = "DeletionStatus"."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "FileDetail"
                    WHERE "FileDetail"."DeletionStatusId" = "DeletionStatus"."Id"
                );
                """);

            migrationBuilder.Sql("DELETE FROM \"FileAccessDetail\" WHERE \"FileDetailId\" IS NULL;");
            migrationBuilder.Sql("DELETE FROM \"DeletionStatus\" WHERE \"FileDetailId\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "FileDetailId",
                table: "FileAccessDetail",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "FileDetailId",
                table: "DeletionStatus",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "DeletionStatusId",
                table: "FileDetail");

            migrationBuilder.DropColumn(
                name: "FileAccessDetailId",
                table: "FileDetail");

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessDetail_FileDetailId",
                table: "FileAccessDetail",
                column: "FileDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeletionStatus_FileDetailId",
                table: "DeletionStatus",
                column: "FileDetailId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DeletionStatus_FileDetail_FileDetailId",
                table: "DeletionStatus",
                column: "FileDetailId",
                principalTable: "FileDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileAccessDetail_FileDetail_FileDetailId",
                table: "FileAccessDetail",
                column: "FileDetailId",
                principalTable: "FileDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeletionStatus_FileDetail_FileDetailId",
                table: "DeletionStatus");

            migrationBuilder.DropForeignKey(
                name: "FK_FileAccessDetail_FileDetail_FileDetailId",
                table: "FileAccessDetail");

            migrationBuilder.DropIndex(
                name: "IX_FileAccessDetail_FileDetailId",
                table: "FileAccessDetail");

            migrationBuilder.DropIndex(
                name: "IX_DeletionStatus_FileDetailId",
                table: "DeletionStatus");

            migrationBuilder.AddColumn<int>(
                name: "DeletionStatusId",
                table: "FileDetail",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FileAccessDetailId",
                table: "FileDetail",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "FileDetail"
                SET "FileAccessDetailId" = (
                    SELECT "Id"
                    FROM "FileAccessDetail"
                    WHERE "FileAccessDetail"."FileDetailId" = "FileDetail"."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "FileAccessDetail"
                    WHERE "FileAccessDetail"."FileDetailId" = "FileDetail"."Id"
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "FileDetail"
                SET "DeletionStatusId" = (
                    SELECT "Id"
                    FROM "DeletionStatus"
                    WHERE "DeletionStatus"."FileDetailId" = "FileDetail"."Id"
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM "DeletionStatus"
                    WHERE "DeletionStatus"."FileDetailId" = "FileDetail"."Id"
                );
                """);

            migrationBuilder.Sql("DELETE FROM \"FileDetail\" WHERE \"FileAccessDetailId\" IS NULL OR \"DeletionStatusId\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "DeletionStatusId",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FileAccessDetailId",
                table: "FileDetail",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_DeletionStatusId",
                table: "FileDetail",
                column: "DeletionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileAccessDetailId",
                table: "FileDetail",
                column: "FileAccessDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileDetail_DeletionStatus_DeletionStatusId",
                table: "FileDetail",
                column: "DeletionStatusId",
                principalTable: "DeletionStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileDetail_FileAccessDetail_FileAccessDetailId",
                table: "FileDetail",
                column: "FileAccessDetailId",
                principalTable: "FileAccessDetail",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "FileDetailId",
                table: "FileAccessDetail");

            migrationBuilder.DropColumn(
                name: "FileDetailId",
                table: "DeletionStatus");
        }
    }
}
