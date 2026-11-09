using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarDev.CloudSync.Accounts.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderAccountKey = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    DriveId = table.Column<string>(type: "TEXT", nullable: false),
                    QuotaUsedBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    QuotaTotalBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.CheckConstraint("CK_Accounts_DriveId_NotBlank", "length(trim(\"DriveId\")) > 0");
                    table.CheckConstraint("CK_Accounts_Provider_Known", "\"Provider\" IN ('OneDrive')");
                    table.CheckConstraint("CK_Accounts_ProviderAccountKey_NotBlank", "length(trim(\"ProviderAccountKey\")) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Provider_ProviderAccountKey",
                table: "Accounts",
                columns: new[] { "Provider", "ProviderAccountKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
