using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.FilesDb.Migrations
{
    /// <inheritdoc />
    public partial class AddStrongIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "files");

            migrationBuilder.CreateTable(
                name: "DeletionStatus",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftDeleted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SoftDeletePending = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HardDeletePending = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventOccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DirectoryName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Handle = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileCreated = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FileLastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    EventName = table.Column<string>(type: "TEXT", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileAccessDetail",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DetailsLastUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastViewed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MoveRequired = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileAccessDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileClassification",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Celebrity = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileClassification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImageDetail",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImageWidth = table.Column<int>(type: "INTEGER", nullable: true),
                    ImageHeight = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageDetail", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelToIgnore",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelToIgnore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagToIgnore",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IgnoreImage = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagToIgnore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileNamePart",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    IncludeInSearch = table.Column<bool>(type: "INTEGER", nullable: false),
                    FileClassificationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "FileDetail",
                schema: "files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileAccessDetailId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageDetailId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    IsImage = table.Column<bool>(type: "INTEGER", nullable: false),
                    FileHandle = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    DeletionStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectoryName = table.Column<string>(type: "nvarchar(256)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileDetail_DeletionStatus_DeletionStatusId",
                        column: x => x.DeletionStatusId,
                        principalSchema: "files",
                        principalTable: "DeletionStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileDetail_FileAccessDetail_FileAccessDetailId",
                        column: x => x.FileAccessDetailId,
                        principalSchema: "files",
                        principalTable: "FileAccessDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileDetail_ImageDetail_ImageDetailId",
                        column: x => x.ImageDetailId,
                        principalSchema: "files",
                        principalTable: "ImageDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileClassificationFileDetail",
                schema: "files",
                columns: table => new
                {
                    FileClassificationsId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileDetailsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileClassificationFileDetail", x => new { x.FileClassificationsId, x.FileDetailsId });
                    table.ForeignKey(
                        name: "FK_FileClassificationFileDetail_FileClassification_FileClassificationsId",
                        column: x => x.FileClassificationsId,
                        principalSchema: "files",
                        principalTable: "FileClassification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileClassificationFileDetail_FileDetail_FileDetailsId",
                        column: x => x.FileDetailsId,
                        principalSchema: "files",
                        principalTable: "FileDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileClassification_Name",
                schema: "files",
                table: "FileClassification",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileClassificationFileDetail_FileDetailsId",
                schema: "files",
                table: "FileClassificationFileDetail",
                column: "FileDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_DeletionStatusId",
                schema: "files",
                table: "FileDetail",
                column: "DeletionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_DuplicateImages",
                schema: "files",
                table: "FileDetail",
                columns: new[] { "IsImage", "FileSize" });

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileAccessDetailId",
                schema: "files",
                table: "FileDetail",
                column: "FileAccessDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileHandle",
                schema: "files",
                table: "FileDetail",
                column: "FileHandle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_FileSize",
                schema: "files",
                table: "FileDetail",
                column: "FileSize");

            migrationBuilder.CreateIndex(
                name: "IX_FileDetail_ImageDetailId",
                schema: "files",
                table: "FileDetail",
                column: "ImageDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_FileNamePart_FileClassificationId",
                schema: "files",
                table: "FileNamePart",
                column: "FileClassificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Event",
                schema: "files");

            migrationBuilder.DropTable(
                name: "FileClassificationFileDetail",
                schema: "files");

            migrationBuilder.DropTable(
                name: "FileNamePart",
                schema: "files");

            migrationBuilder.DropTable(
                name: "ModelToIgnore",
                schema: "files");

            migrationBuilder.DropTable(
                name: "TagToIgnore",
                schema: "files");

            migrationBuilder.DropTable(
                name: "FileDetail",
                schema: "files");

            migrationBuilder.DropTable(
                name: "FileClassification",
                schema: "files");

            migrationBuilder.DropTable(
                name: "DeletionStatus",
                schema: "files");

            migrationBuilder.DropTable(
                name: "FileAccessDetail",
                schema: "files");

            migrationBuilder.DropTable(
                name: "ImageDetail",
                schema: "files");
        }
    }
}
