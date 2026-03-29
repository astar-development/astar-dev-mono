using System.Reflection;
using AStar.Dev.OneDriveSync.old.Models;
using AStar.Dev.OneDriveSync.old.Services;

namespace AStar.Dev.OneDriveSync.old.Tests.Unit.Services;

[TestSubject(typeof(JsonAccountStore))]
public sealed class JsonAccountStoreShould : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;
    private readonly JsonAccountStore _sut;

    public JsonAccountStoreShould()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"astar-test-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "accounts.json");

        _sut = new JsonAccountStore();
        FieldInfo field = typeof(JsonAccountStore).GetField("_filePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(_sut, _filePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ReturnEmptyList_WhenFileDoesNotExist()
    {
        IReadOnlyList<AccountRecord> accounts = await _sut.LoadAsync(TestContext.Current.CancellationToken);
        accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task RoundTripAccounts()
    {
        var accounts = new List<AccountRecord>
        {
            new()
            {
                AccountId = "a1",
                Email = "test@example.com",
                DisplayName = "Test User",
                LocalSyncPath = "/home/test/OneDrive/test",
                SyncIntervalMinutes = 30,
                SelectedFolders = [new SelectedFolder { FolderId = "f1", Name = "Documents" }]
            }
        };

        await _sut.SaveAsync(accounts, TestContext.Current.CancellationToken);
        IReadOnlyList<AccountRecord> loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        loaded.Count.ShouldBe(1);
        loaded[0].AccountId.ShouldBe("a1");
        loaded[0].Email.ShouldBe("test@example.com");
        loaded[0].SyncIntervalMinutes.ShouldBe(30);
        loaded[0].SelectedFolders.Count.ShouldBe(1);
        loaded[0].SelectedFolders[0].Name.ShouldBe("Documents");
    }
}
