using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.ViewModels.SyncPairs;
using AStarDev.FunctionalParadigm;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace AStarDev.CloudSync.TestsUnit.SyncPairs;

public class GivenASyncPairDirectionGlyph
{
    public static TheoryData<string> Directions() => new("two-way", "upload-only", "download-only");

    [AvaloniaTheory]
    [MemberData(nameof(Directions))]
    public void when_rendered_then_every_character_is_in_the_inter_font(string direction)
    {
        var pair = CreatePair(direction switch
        {
            "upload-only" => SyncDirectionFactory.CreateUploadOnly(),
            "download-only" => SyncDirectionFactory.CreateDownloadOnly(),
            _ => SyncDirectionFactory.CreateTwoWay()
        });
        FontManager.Current.TryGetGlyphTypeface(new Typeface(new FontFamily("fonts:Inter#Inter")), out var inter).ShouldBeTrue();

        var missing = pair.DirectionGlyph.EnumerateRunes().Where(rune => !inter!.CharacterToGlyphMap.ContainsGlyph(rune.Value)).Select(rune => $"U+{rune.Value:X4}").ToList();

        missing.ShouldBeEmpty();
    }

    private static SyncPairViewModel CreatePair(SyncDirection direction)
    {
        var account = TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), Option.None<DateTimeOffset>());
        var model = SyncPairFactory.Create(SyncPairIdFactory.Create(Guid.NewGuid()), account.Id, "Documents", new DirectoryInfo("/home/user/CloudSync/OneDrive/Work/Documents"), "/Documents", direction, SyncStatusFactory.CreateUpToDate(), 1_118, Option.Some(TestData.Now.AddMinutes(-4)));

        return new SyncPairViewModel(model, account, TestData.CreateLocalization(), TestData.CreateTime());
    }
}
