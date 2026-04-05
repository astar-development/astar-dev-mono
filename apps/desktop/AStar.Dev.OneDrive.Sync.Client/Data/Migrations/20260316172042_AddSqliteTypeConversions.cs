using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations;

/// <inheritdoc />
public partial class AddSqliteTypeConversions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameColumn(
            name: "RemoteModified",
            table: "SyncJobs",
            newName: "RemoteModified_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "QueuedAt",
            table: "SyncJobs",
            newName: "QueuedAt_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "CompletedAt",
            table: "SyncJobs",
            newName: "CompletedAt_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "ResolvedAt",
            table: "SyncConflicts",
            newName: "ResolvedAt_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "RemoteModified",
            table: "SyncConflicts",
            newName: "RemoteModified_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "LocalModified",
            table: "SyncConflicts",
            newName: "LocalModified_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "DetectedAt",
            table: "SyncConflicts",
            newName: "DetectedAt_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "LastSyncedAt",
            table: "Accounts",
            newName: "LastSyncedAt_Ticks");

        _ = migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "SyncJobs",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "RemoteModified_Ticks",
            table: "SyncJobs",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "QueuedAt_Ticks",
            table: "SyncJobs",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "CompletedAt_Ticks",
            table: "SyncJobs",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<byte[]>(
            name: "Id",
            table: "SyncConflicts",
            type: "BLOB",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "ResolvedAt_Ticks",
            table: "SyncConflicts",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<long>(
            name: "RemoteModified_Ticks",
            table: "SyncConflicts",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "LocalModified_Ticks",
            table: "SyncConflicts",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "DetectedAt_Ticks",
            table: "SyncConflicts",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "LastSyncedAt_Ticks",
            table: "Accounts",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameColumn(
            name: "RemoteModified_Ticks",
            table: "SyncJobs",
            newName: "RemoteModified");

        _ = migrationBuilder.RenameColumn(
            name: "QueuedAt_Ticks",
            table: "SyncJobs",
            newName: "QueuedAt");

        _ = migrationBuilder.RenameColumn(
            name: "CompletedAt_Ticks",
            table: "SyncJobs",
            newName: "CompletedAt");

        _ = migrationBuilder.RenameColumn(
            name: "ResolvedAt_Ticks",
            table: "SyncConflicts",
            newName: "ResolvedAt");

        _ = migrationBuilder.RenameColumn(
            name: "RemoteModified_Ticks",
            table: "SyncConflicts",
            newName: "RemoteModified");

        _ = migrationBuilder.RenameColumn(
            name: "LocalModified_Ticks",
            table: "SyncConflicts",
            newName: "LocalModified");

        _ = migrationBuilder.RenameColumn(
            name: "DetectedAt_Ticks",
            table: "SyncConflicts",
            newName: "DetectedAt");

        _ = migrationBuilder.RenameColumn(
            name: "LastSyncedAt_Ticks",
            table: "Accounts",
            newName: "LastSyncedAt");

        _ = migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "RemoteModified",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "QueuedAt",
            table: "SyncJobs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CompletedAt",
            table: "SyncJobs",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "INTEGER",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(byte[]),
            oldType: "BLOB");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "ResolvedAt",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "INTEGER",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "RemoteModified",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LocalModified",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "DetectedAt",
            table: "SyncConflicts",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastSyncedAt",
            table: "Accounts",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "INTEGER",
            oldNullable: true);
    }
}
