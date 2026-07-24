using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations;

/// <inheritdoc />
public partial class UnifyFileClassificationHierarchy : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DownloadedFileClassification_FileClassification_FileClassificationId",
            schema: "files",
            table: "DownloadedFileClassification");

        migrationBuilder.DropTable(
            name: "FileNamePart",
            schema: "files");

        migrationBuilder.RenameColumn(
            name: "Celebrity",
            schema: "files",
            table: "FileClassification",
            newName: "IsFamous");

        migrationBuilder.AddColumn<int>(
            name: "Level",
            schema: "files",
            table: "FileClassification",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("UPDATE FileClassification SET Level = 3");

        migrationBuilder.RenameColumn(
            name: "FileClassificationId",
            schema: "files",
            table: "DownloadedFileClassification",
            newName: "CategoryId");

        migrationBuilder.RenameIndex(
            name: "IX_DownloadedFileClassification_FileDetailId_FileClassificationId",
            schema: "files",
            table: "DownloadedFileClassification",
            newName: "IX_DownloadedFileClassification_FileDetailId_CategoryId");

        migrationBuilder.RenameIndex(
            name: "IX_DownloadedFileClassification_FileClassificationId",
            schema: "files",
            table: "DownloadedFileClassification",
            newName: "IX_DownloadedFileClassification_CategoryId");

        migrationBuilder.AddColumn<int>(
            name: "ParentId",
            schema: "files",
            table: "FileClassification",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "FileClassificationKeyword",
            schema: "files",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Keyword = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileClassificationKeyword", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileClassificationKeyword_FileClassification_CategoryId",
                    column: x => x.CategoryId,
                    principalSchema: "files",
                    principalTable: "FileClassification",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FileClassification_ParentId",
            schema: "files",
            table: "FileClassification",
            column: "ParentId");

        migrationBuilder.CreateIndex(
            name: "IX_FileClassificationKeyword_CategoryId_Keyword",
            schema: "files",
            table: "FileClassificationKeyword",
            columns: ["CategoryId", "Keyword"],
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_DownloadedFileClassification_FileClassification_CategoryId",
            schema: "files",
            table: "DownloadedFileClassification",
            column: "CategoryId",
            principalSchema: "files",
            principalTable: "FileClassification",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_FileClassification_FileClassification_ParentId",
            schema: "files",
            table: "FileClassification",
            column: "ParentId",
            principalSchema: "files",
            principalTable: "FileClassification",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DownloadedFileClassification_FileClassification_CategoryId",
            schema: "files",
            table: "DownloadedFileClassification");

        migrationBuilder.DropForeignKey(
            name: "FK_FileClassification_FileClassification_ParentId",
            schema: "files",
            table: "FileClassification");

        migrationBuilder.DropTable(
            name: "FileClassificationKeyword",
            schema: "files");

        migrationBuilder.DropIndex(
            name: "IX_FileClassification_ParentId",
            schema: "files",
            table: "FileClassification");

        migrationBuilder.DropColumn(
            name: "ParentId",
            schema: "files",
            table: "FileClassification");

        migrationBuilder.DropColumn(
            name: "Level",
            schema: "files",
            table: "FileClassification");

        migrationBuilder.RenameColumn(
            name: "IsFamous",
            schema: "files",
            table: "FileClassification",
            newName: "Celebrity");

        migrationBuilder.RenameColumn(
            name: "CategoryId",
            schema: "files",
            table: "DownloadedFileClassification",
            newName: "FileClassificationId");

        migrationBuilder.RenameIndex(
            name: "IX_DownloadedFileClassification_FileDetailId_CategoryId",
            schema: "files",
            table: "DownloadedFileClassification",
            newName: "IX_DownloadedFileClassification_FileDetailId_FileClassificationId");

        migrationBuilder.RenameIndex(
            name: "IX_DownloadedFileClassification_CategoryId",
            schema: "files",
            table: "DownloadedFileClassification",
            newName: "IX_DownloadedFileClassification_FileClassificationId");

        migrationBuilder.CreateTable(
            name: "FileNamePart",
            schema: "files",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                CreatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                FileClassificationId = table.Column<int>(type: "INTEGER", nullable: true),
                IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                UpdatedAt_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileNamePart", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileNamePart_FileClassification_FileClassificationId",
                    column: x => x.FileClassificationId,
                    principalSchema: "files",
                    principalTable: "FileClassification",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_FileNamePart_FileClassificationId",
            schema: "files",
            table: "FileNamePart",
            column: "FileClassificationId");

        migrationBuilder.AddForeignKey(
            name: "FK_DownloadedFileClassification_FileClassification_FileClassificationId",
            schema: "files",
            table: "DownloadedFileClassification",
            column: "FileClassificationId",
            principalSchema: "files",
            principalTable: "FileClassification",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
