using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Overview;

public sealed class PlaceholderSyncOverviewSource(TimeProvider timeProvider) : ISyncOverviewSource
{
    private const long Gigabyte = 1_073_741_824;
    private const long Terabyte = 1_099_511_627_776;
    private const long BytesMovedToday = 3_328_599_655;

    private static readonly long[] throughputBytesPerSecond = [180_000, 240_000, 210_000, 900_000, 760_000, 2_400_000, 1_900_000, 3_800_000, 3_100_000, 5_600_000, 4_700_000, 7_200_000, 6_100_000, 8_900_000, 7_800_000, 10_400_000, 9_600_000, 11_800_000, 10_900_000, 12_700_000, 12_100_000, 13_900_000, 13_400_000, 14_889_779];

    public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var oneDrive = CloudProviderFactory.CreateOneDrive();

        var work = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Work", "work@example.com", oneDrive, StorageUsageFactory.Create(782_643_000_000, Terabyte), 30, 240_686, SyncStatusFactory.CreateSyncing(25), Option.Some(now.AddMinutes(-4)));
        var personal = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Personal", "me@example.com", oneDrive, StorageUsageFactory.Create(44_238_000_000, 100 * Gigabyte), 12, 9_402, SyncStatusFactory.CreateUpToDate(), Option.Some(now.AddHours(-1)));
        var photos = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Photos", "photos@example.com", oneDrive, StorageUsageFactory.Create(1_718_000_000, 5 * Gigabyte), 3, 1_118, SyncStatusFactory.CreateHasConflicts(2), Option.Some(now.AddHours(-30)));
        var archive = CloudAccountFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), "Archive", "archive@example.com", oneDrive, StorageUsageFactory.Create(1_048_576, 5 * Gigabyte), 3, 0, SyncStatusFactory.CreatePaused(), Option.None<DateTimeOffset>());

        var documents = CreatePair(work, "Documents", "/Documents", SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateSyncing(25), 4_812, Option.Some(now.AddMinutes(-4)));
        var projects = CreatePair(work, "Projects", "/Projects", SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 15_240, Option.Some(now.AddMinutes(-4)));
        var desktop = CreatePair(work, "Desktop", "/Desktop", SyncDirectionFactory.CreateUploadOnly(), SyncStatusFactory.CreateUpToDate(), 320, Option.Some(now.AddHours(-1)));
        var recipes = CreatePair(personal, "Recipes", "/Recipes", SyncDirectionFactory.CreateDownloadOnly(), SyncStatusFactory.CreateUpToDate(), 212, Option.Some(now.AddHours(-1)));
        var music = CreatePair(personal, "Music", "/Music", SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateUpToDate(), 4_120, Option.Some(now.AddHours(-26)));
        var holidayPhotos = CreatePair(photos, "Holiday photos", "/Camera", SyncDirectionFactory.CreateTwoWay(), SyncStatusFactory.CreateHasConflicts(2), 1_118, Option.Some(now.AddHours(-30)));
        var backups = CreatePair(archive, "Backups", "/Backups", SyncDirectionFactory.CreateUploadOnly(), SyncStatusFactory.CreatePaused(), 0, Option.None<DateTimeOffset>());

        ActivityEntry[] activity =
        [
            ActivityEntryFactory.Create(work.Id, Option.Some(documents.Id), ActivityKindFactory.CreateDetectingChanges(), now),
            ActivityEntryFactory.Create(work.Id, Option.Some(documents.Id), ActivityKindFactory.CreateUploaded(12, 88_080_384), now.AddMinutes(-2)),
            ActivityEntryFactory.Create(work.Id, Option.None<SyncPairId>(), ActivityKindFactory.CreateAuthenticated(), now.AddMinutes(-3)),
            ActivityEntryFactory.Create(personal.Id, Option.Some(recipes.Id), ActivityKindFactory.CreateDownloaded(3), now.AddHours(-1)),
            ActivityEntryFactory.Create(photos.Id, Option.Some(holidayPhotos.Id), ActivityKindFactory.CreateConflictDetected("holiday-042.jpg"), now.AddHours(-30)),
            ActivityEntryFactory.Create(photos.Id, Option.Some(holidayPhotos.Id), ActivityKindFactory.CreateConflictDetected("holiday-043.jpg"), now.AddHours(-30))
        ];

        SyncConflict[] conflicts =
        [
            SyncConflictFactory.Create(holidayPhotos.Id, "holiday-042.jpg", now.AddHours(-30)),
            SyncConflictFactory.Create(holidayPhotos.Id, "holiday-043.jpg", now.AddHours(-30))
        ];

        var throughput = throughputBytesPerSecond.Select((rate, index) => ThroughputSampleFactory.Create(now.AddMinutes(-5 * (throughputBytesPerSecond.Length - 1 - index)), rate)).ToArray();

        return Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([work, personal, photos, archive], [documents, projects, desktop, recipes, music, holidayPhotos, backups], activity, conflicts, throughput, BytesMovedToday, Option.Some(now.AddMinutes(-10)))));
    }

    private static SyncPair CreatePair(CloudAccount account, string name, string remotePath, SyncDirection direction, SyncStatus status, int fileCount, Option<DateTimeOffset> lastSync) => SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, name, RelativePaths.Of(remotePath.TrimStart('/')), Option.None<RemoteItemId>(), direction, status, fileCount, lastSync);
}
