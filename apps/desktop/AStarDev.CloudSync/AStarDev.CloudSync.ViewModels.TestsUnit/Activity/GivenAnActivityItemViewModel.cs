using AStarDev.CloudSync.Core.Activity;
using AStarDev.CloudSync.Core.SyncPairs;
using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.CloudSync.Domain.Activity;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Activity;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Activity;

public class GivenAnActivityItemViewModel
{
    public static TheoryData<string, string> KindsAndTexts() => new()
    {
        { "detecting", "Detecting local changes…" },
        { "uploaded-one", "Uploaded 1 file (2.0 KB)" },
        { "uploaded-many", "Uploaded 12 files (84.0 MB)" },
        { "downloaded-one", "Downloaded 1 file" },
        { "downloaded-many", "Downloaded 3 files" },
        { "conflict", "Conflict: holiday-042.jpg" },
        { "authenticated", "Authenticated" }
    };

    [Theory]
    [MemberData(nameof(KindsAndTexts))]
    public void when_created_then_the_text_describes_the_activity(string kindName, string expectedText)
    {
        var item = CreateItem(KindFor(kindName), TestData.Now);

        item.Text.ShouldBe(expectedText);
    }

    [Theory]
    [InlineData(0, "Just now")]
    [InlineData(5, "5 min ago")]
    [InlineData(180, "3 h ago")]
    [InlineData(1500, "Yesterday")]
    [InlineData(4320, "3 days ago")]
    public void when_created_then_the_when_text_is_relative_to_now(int minutesAgo, string expectedText)
    {
        var item = CreateItem(ActivityKindFactory.CreateAuthenticated(), TestData.Now.AddMinutes(-minutesAgo));

        item.WhenText.ShouldBe(expectedText);
    }

    private static ActivityItemViewModel CreateItem(ActivityKind kind, DateTimeOffset occurredAt)
    {
        var entry = ActivityEntryFactory.Create(AccountIdFactory.Create(Guid.NewGuid()), Option.None<SyncPairId>(), kind, occurredAt);
        var localization = TestData.CreateLocalization();

        return new ActivityItemViewModel(entry, new ProviderViewModel(CloudProviderFactory.CreateOneDrive(), localization), localization, TestData.CreateTime());
    }

    private static ActivityKind KindFor(string kindName) => kindName switch
    {
        "detecting" => ActivityKindFactory.CreateDetectingChanges(),
        "uploaded-one" => ActivityKindFactory.CreateUploaded(1, 2048),
        "uploaded-many" => ActivityKindFactory.CreateUploaded(12, 88_080_384),
        "downloaded-one" => ActivityKindFactory.CreateDownloaded(1),
        "downloaded-many" => ActivityKindFactory.CreateDownloaded(3),
        "conflict" => ActivityKindFactory.CreateConflictDetected("holiday-042.jpg"),
        _ => ActivityKindFactory.CreateAuthenticated()
    };
}
