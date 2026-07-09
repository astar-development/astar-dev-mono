using AStar.Dev.Infrastructure.AppDb;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Serilog.Core;

namespace AStar.Dev.Wallpaper.Scraper.Services;

public class DatabaseInitializationService(IDbContextFactory<AppDbContext> contextFactory, Logger logger)
{
    public async Task InitialiseAsync()
    {
        await using var context = contextFactory.CreateDbContext();

        await EnsureMigrationHistoryIsAlignedAsync(context);

        await context.Database.MigrateAsync();

        await DataSeed.SeedTagsToIgnoreAsync(logger, context);
        await DataSeed.SeedScrapeConfigurationAsync(logger, context);

        string csvPath = Path.Combine(ApplicationMetadata.ApplicationFolder, "Mappings.csv");
        await DataSeed.SeedFileClassificationsAsync(csvPath, logger, context);
    }

    private async Task EnsureMigrationHistoryIsAlignedAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        if (!await TableExistsAsync(connection, "__EFMigrationsHistory"))
            return;

        string productVersion = await GetProductVersionForHistoryAsync(connection);

        if (await ShouldBackfillAddScraperTablesMigrationAsync(connection))
        {
            await InsertMigrationHistoryAsync(connection, "20260702234641_AddScraperTables", productVersion);
            logger.Information("Backfilled migration history row for 20260702234641_AddScraperTables.");
        }

        if (await ShouldBackfillMergeDownloadedMigrationAsync(connection))
        {
            await InsertMigrationHistoryAsync(connection, "20260703184100_MergeDownloadedFileClassifications", productVersion);
            logger.Information("Backfilled migration history row for 20260703184100_MergeDownloadedFileClassifications.");
        }

        if (await ShouldBackfillUnifySingleParentMigrationAsync(connection))
        {
            await InsertMigrationHistoryAsync(connection, "20260703200740_UnifyFileClassificationsSingleParent", productVersion);
            logger.Information("Backfilled migration history row for 20260703200740_UnifyFileClassificationsSingleParent.");
        }
    }

    private static async Task<bool> ShouldBackfillMergeDownloadedMigrationAsync(DbConnection connection)
    {
        if (await MigrationExistsAsync(connection, "20260703184100_MergeDownloadedFileClassifications"))
            return false;

        return await TableExistsAsync(connection, "SyncedItemFileClassifications")
            && await ColumnExistsAsync(connection, "SyncedItemFileClassifications", "FileDetailId")
            && !await TableExistsAsync(connection, "DownloadedFileClassifications");
    }

    private static async Task<bool> ShouldBackfillAddScraperTablesMigrationAsync(DbConnection connection)
    {
        if (await MigrationExistsAsync(connection, "20260702234641_AddScraperTables"))
            return false;

        bool hasLegacyShape = await ColumnExistsAsync(connection, "FileDetail", "DeletionStatusId");
        bool hasNaturalCascadeShape = await ColumnExistsAsync(connection, "DeletionStatus", "FileDetailId");

        return await TableExistsAsync(connection, "DeletionStatus")
            && await TableExistsAsync(connection, "FileAccessDetail")
            && await TableExistsAsync(connection, "ImageDetail")
            && await TableExistsAsync(connection, "FileDetail")
            && (hasLegacyShape || hasNaturalCascadeShape);
    }

    private static async Task<bool> ShouldBackfillUnifySingleParentMigrationAsync(DbConnection connection)
    {
        if (await MigrationExistsAsync(connection, "20260703200740_UnifyFileClassificationsSingleParent"))
            return false;

        return await TableExistsAsync(connection, "FileClassifications")
            && await ColumnExistsAsync(connection, "SyncedItems", "FileDetailId")
            && !await TableExistsAsync(connection, "SyncedItemFileClassifications");
    }

    private static async Task<bool> MigrationExistsAsync(DbConnection connection, string migrationId)
        => await ExecuteScalarLongAsync(connection, "SELECT COUNT(1) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @migrationId;", ("@migrationId", migrationId)) > 0;

    private static async Task<string> GetProductVersionForHistoryAsync(DbConnection connection)
    {
        string? version = await ExecuteScalarStringAsync(connection, "SELECT \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1;");

        if (!string.IsNullOrWhiteSpace(version))
            return version;

        return typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+')[0]
            ?? "10.0.0";
    }

    private static async Task InsertMigrationHistoryAsync(DbConnection connection, string migrationId, string productVersion)
        => _ = await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@migrationId, @productVersion);",
            ("@migrationId", migrationId),
            ("@productVersion", productVersion));

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
        => await ExecuteScalarLongAsync(connection, "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @tableName;", ("@tableName", tableName)) > 0;

    private static async Task<bool> ColumnExistsAsync(DbConnection connection, string tableName, string columnName)
        => await ExecuteScalarLongAsync(connection, "SELECT COUNT(1) FROM pragma_table_info(@tableName) WHERE name = @columnName;", ("@tableName", tableName), ("@columnName", columnName)) > 0;

    private static async Task<long> ExecuteScalarLongAsync(DbConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        object? result = await command.ExecuteScalarAsync();

        return result is null or DBNull ? 0L : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task<string?> ExecuteScalarStringAsync(DbConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        object? result = await command.ExecuteScalarAsync();

        return result is null or DBNull ? null : Convert.ToString(result, CultureInfo.InvariantCulture);
    }

    private static async Task<int> ExecuteNonQueryAsync(DbConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(connection, sql, parameters);

        return await command.ExecuteNonQueryAsync();
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
