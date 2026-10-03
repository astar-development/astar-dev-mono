using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.ControlDb.Migrations
{
    /// <inheritdoc />
    public partial class AddFilesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "SearchConfigurations",
                newName: "UpdatedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "SearchConfigurations",
                newName: "CreatedAt_Ticks");

            migrationBuilder.AlterColumn<long>(
                name: "UpdatedAt_Ticks",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "CreatedAt_Ticks",
                table: "SearchConfigurations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "FileDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileHandle = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    IsImage = table.Column<bool>(type: "INTEGER", nullable: false),
                    DirectoryName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    FileName = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeletionStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SoftDeleted_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    SoftDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    HardDeletePending_Ticks = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeletionStatus_FileDetail_FileId",
                        column: x => x.FileId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileAccessDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DetailsLastUpdated_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    LastViewed_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    MoveRequired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccessDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileAccessDetail_FileDetail_FileId",
                        column: x => x.FileId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImageDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImageDetail_FileDetail_FileId",
                        column: x => x.FileId,
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletionStatus_FileId",
                table: "DeletionStatus",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileAccessDetail_FileId",
                table: "FileAccessDetail",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_DuplicateImages",
                table: "FileDetail",
                columns: ["IsImage", "FileSize"]);

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileHandle",
                table: "FileDetail",
                column: "FileHandle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileSize",
                table: "FileDetail",
                column: "FileSize");

            migrationBuilder.CreateIndex(
                name: "IX_ImageDetail_FileId",
                table: "ImageDetail",
                column: "FileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletionStatus");

            migrationBuilder.DropTable(
                name: "FileAccessDetail");

            migrationBuilder.DropTable(
                name: "ImageDetail");

            migrationBuilder.DropTable(
                name: "FileDetail");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt_Ticks",
                table: "SearchConfigurations",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAt_Ticks",
                table: "SearchConfigurations",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "SearchConfigurations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
