using AStarDev.FunctionalParadigm;
using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASyncBaselineEntryFactory
{
    [Fact]
    public void when_a_file_has_a_negative_size_then_the_size_becomes_zero()
    {
        var kind = SyncBaselineKindFactory.CreateFile(-5, DateTimeOffset.UnixEpoch, Hash("h"), Tag("c"), ETag("e"));

        kind.ShouldBeOfType<SyncBaselineKind.File>().Size.ShouldBe(0);
    }

    [Fact]
    public void when_a_file_has_a_local_offset_time_then_it_is_stored_in_utc()
    {
        var local = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.FromHours(5));

        var kind = SyncBaselineKindFactory.CreateFile(1, local, Hash("h"), Tag("c"), ETag("e"));

        kind.ShouldBeOfType<SyncBaselineKind.File>().LastWriteUtc.ShouldBe(new DateTimeOffset(2026, 11, 1, 7, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void when_an_entry_is_created_then_it_carries_the_given_values()
    {
        var account = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var path = RelativeSyncPathFactory.Create("Docs/a.txt").Match(created => created, _ => throw new InvalidOperationException("The test path must be valid."));
        var itemId = RemoteItemIdFactory.Create("item-1").Match(created => created, _ => throw new InvalidOperationException("The test id must be valid."));

        var entry = SyncBaselineEntryFactory.Create(account, path, itemId, SyncBaselineKindFactory.CreateFolder());

        (entry.AccountId, entry.Path, entry.ItemId, entry.Kind).ShouldBe((account, path, itemId, SyncBaselineKindFactory.CreateFolder()));
    }

    private static ContentHash Hash(string value) => ContentHashFactory.Create(value).Match(created => created, _ => throw new InvalidOperationException("The test hash must be valid."));

    private static ContentTag Tag(string value) => ContentTagFactory.Create(value).Match(created => created, _ => throw new InvalidOperationException("The test tag must be valid."));

    private static EntityTag ETag(string value) => EntityTagFactory.Create(value).Match(created => created, _ => throw new InvalidOperationException("The test tag must be valid."));
}
