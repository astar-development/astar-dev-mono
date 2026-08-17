using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.Infrastructure.AppDb.Migrations;

/// <inheritdoc />
public partial class AddNoCaseToDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "SessionCookie",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Password",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "LoginEmailAddress",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Value",
            table: "TagToIgnore",
            type: "TEXT",
            maxLength: 300,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 300);

        migrationBuilder.AlterColumn<string>(
            name: "RemotePath",
            table: "SyncRules",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "RelativePath",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "LocalPath",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "RemotePath",
            table: "SyncedItems",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "RemoteParentId",
            table: "SyncedItems",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "LocalPath",
            table: "SyncedItems",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "RelativePath",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "LocalPath",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "TopWallpapers",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Subscriptions",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "SearchStringSuffix",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "SearchStringPrefix",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "SearchString",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "ApiKey",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "SearchCategories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Id",
            table: "SearchCategories",
            type: "TEXT",
            maxLength: 128,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 128);

        migrationBuilder.AlterColumn<string>(
            name: "SubDirectoryName",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "RootDirectory",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "BaseSaveDirectory",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "BaseDirectoryFamous",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "BaseDirectory",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Value",
            table: "ModelToIgnore",
            type: "TEXT",
            maxLength: 300,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 300);

        migrationBuilder.AlterColumn<string>(
            name: "FileName",
            table: "FileDetail",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "DirectoryName",
            table: "FileDetail",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "Keyword",
            table: "FileClassificationKeywords",
            type: "TEXT",
            maxLength: 150,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 150);

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "FileClassificationCategories",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "UpdatedBy",
            table: "Event",
            type: "TEXT",
            maxLength: 30,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 30);

        migrationBuilder.AlterColumn<string>(
            name: "Handle",
            table: "Event",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "FileName",
            table: "Event",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "DirectoryName",
            table: "Event",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Sqlite",
            table: "ConnectionStrings",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256);

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Accounts",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");

        migrationBuilder.AlterColumn<string>(
            name: "DisplayName",
            table: "Accounts",
            type: "TEXT",
            nullable: false,
            collation: "NOCASE",
            oldClrType: typeof(string),
            oldType: "TEXT");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "SessionCookie",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Password",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "LoginEmailAddress",
            table: "UserConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Value",
            table: "TagToIgnore",
            type: "TEXT",
            maxLength: 300,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 300,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "RemotePath",
            table: "SyncRules",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "RelativePath",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "LocalPath",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "RemotePath",
            table: "SyncedItems",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "RemoteParentId",
            table: "SyncedItems",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "LocalPath",
            table: "SyncedItems",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "RelativePath",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "LocalPath",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "TopWallpapers",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Subscriptions",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "SearchStringSuffix",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "SearchStringPrefix",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "SearchString",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "ApiKey",
            table: "SearchConfiguration",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "SearchCategories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Id",
            table: "SearchCategories",
            type: "TEXT",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 128,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "SubDirectoryName",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "RootDirectory",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "BaseSaveDirectory",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "BaseDirectoryFamous",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "BaseDirectory",
            table: "ScrapeDirectories",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Value",
            table: "ModelToIgnore",
            type: "TEXT",
            maxLength: 300,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 300,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "FileName",
            table: "FileDetail",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "DirectoryName",
            table: "FileDetail",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Keyword",
            table: "FileClassificationKeywords",
            type: "TEXT",
            maxLength: 150,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 150,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "FileClassificationCategories",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "UpdatedBy",
            table: "Event",
            type: "TEXT",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 30,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Handle",
            table: "Event",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "FileName",
            table: "Event",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "DirectoryName",
            table: "Event",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Sqlite",
            table: "ConnectionStrings",
            type: "TEXT",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 256,
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Accounts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");

        migrationBuilder.AlterColumn<string>(
            name: "DisplayName",
            table: "Accounts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldCollation: "NOCASE");
    }
}
