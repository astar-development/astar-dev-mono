namespace AStar.Dev.Conflict.Resolution.Tests.Unit;

public sealed class JsonConflictStoreTests : IAsyncDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"conflict-store-test-{Guid.NewGuid()}.json");

    public ValueTask DisposeAsync()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task LoadAsync_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        var store = new JsonConflictStore(_tempFile);

        var result = await store.LoadAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsRecords()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new JsonConflictStore(_tempFile);
        var records = new List<ConflictRecord>
        {
            new()
            {
                FileName = "file.txt",
                FilePath = "docs/file.txt",
                ConflictType = ConflictType.ModifiedBothSides,
                LocalModifiedUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                RemoteModifiedUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
                LocalSizeBytes = 1024,
                RemoteSizeBytes = 2048
            },
            new()
            {
                FileName = "other.txt",
                FilePath = "docs/other.txt",
                ConflictType = ConflictType.DeletedLocalPresentRemote,
                ResolvedPolicy = ConflictPolicy.Skip,
                ResolvedAtUtc = DateTimeOffset.UtcNow
            }
        };

        await store.SaveAsync(records, ct);
        var loaded = await store.LoadAsync(ct);

        loaded.Count.ShouldBe(2);
        loaded[0].FileName.ShouldBe("file.txt");
        loaded[0].ConflictType.ShouldBe(ConflictType.ModifiedBothSides);
        loaded[0].LocalSizeBytes.ShouldBe(1024);
        loaded[1].FileName.ShouldBe("other.txt");
        loaded[1].ResolvedPolicy.ShouldBe(ConflictPolicy.Skip);
        loaded[1].IsResolved.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_CreatesDirectoryIfMissing()
    {
        var ct = TestContext.Current.CancellationToken;
        var nestedPath = Path.Combine(Path.GetTempPath(), $"conflict-test-{Guid.NewGuid()}", "data", "conflicts.json");
        var store = new JsonConflictStore(nestedPath);

        await store.SaveAsync([new ConflictRecord { FileName = "test.txt", FilePath = "test.txt" }], ct);

        File.Exists(nestedPath).ShouldBeTrue();

        // Clean up nested dirs
        Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(nestedPath))!, true);
    }
}
