namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.DataMigration;

/// <summary>Migrates text-based classification rows from the legacy <c>SyncedItemClassifications</c> table into the <c>SyncedItemFileClassifications</c> junction table.</summary>
public interface IClassificationDataMigrationService
{
    /// <summary>Runs the one-time data migration. No-ops if the junction table already has rows or if the legacy table no longer exists.</summary>
    Task MigrateAsync(CancellationToken cancellationToken);
}
