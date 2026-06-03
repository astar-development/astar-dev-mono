using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Conflicts;

public sealed class GivenAConflictItemViewModel
{
    private static SyncConflict BuildConflict() => new()
    {
        Remote   = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789")),
        Target   = SyncFileTargetFactory.Create("/home/user/docs/report.pdf", "docs/report.pdf"),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddHours(-1), 1024L, DateTimeOffset.UtcNow, 2048L),
    };

    private static ILocalizationService BuildLocalizationService()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetLocal(Arg.Any<string>()).Returns(x => x.ArgAt<string>(0));

        return loc;
    }

    [Fact]
    public void when_constructed_then_policy_options_labels_are_retrieved_via_localisation_service()
    {
        var loc = BuildLocalizationService();
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);

        loc.Received(1).GetLocal("ConflictPolicy.Ignore");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins");
    }

    [Fact]
    public void when_constructed_then_policy_options_descriptions_are_retrieved_via_localisation_service()
    {
        var loc = BuildLocalizationService();
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);

        loc.Received(1).GetLocal("ConflictPolicy.Ignore.Description");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins.Description");
    }

    [Fact]
    public void when_culture_changed_then_policy_options_is_rebuilt()
    {
        var loc = BuildLocalizationService();
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("ConflictPolicy.Ignore");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins");
    }

    [Fact]
    public void when_culture_changed_then_policy_options_descriptions_are_rebuilt()
    {
        var loc = BuildLocalizationService();
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);
        loc.ClearReceivedCalls();

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        loc.Received(1).GetLocal("ConflictPolicy.Ignore.Description");
        loc.Received(1).GetLocal("ConflictPolicy.KeepBoth.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LastWriteWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.LocalWins.Description");
        loc.Received(1).GetLocal("ConflictPolicy.RemoteWins.Description");
    }

    [Fact]
    public void when_culture_changed_then_property_changed_fires_for_policy_options()
    {
        var loc = BuildLocalizationService();
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        firedProperties.ShouldContain(nameof(sut.PolicyOptions));
    }

    [Fact]
    public void when_is_expanded_is_true_then_collapse_expand_label_uses_collapse_key()
    {
        var loc = BuildLocalizationService();
        loc.GetLocal("Conflict.Collapse").Returns("test-collapse");
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);

        sut.IsExpanded = true;

        sut.CollapseExpandLabel.ShouldBe("test-collapse");
    }

    [Fact]
    public void when_is_expanded_is_false_then_collapse_expand_label_uses_resolve_key()
    {
        var loc = BuildLocalizationService();
        loc.GetLocal("Conflict.Resolve").Returns("test-resolve");
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);

        sut.IsExpanded = false;

        sut.CollapseExpandLabel.ShouldBe("test-resolve");
    }

    [Fact]
    public void when_culture_changed_then_property_changed_fires_for_collapse_expand_label()
    {
        var loc = BuildLocalizationService();
        var sut = new ConflictItemViewModel(BuildConflict(), Substitute.For<ISyncService>(), loc);
        var firedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => firedProperties.Add(args.PropertyName);

        loc.CultureChanged += Raise.Event<EventHandler<CultureInfo>>(new object(), CultureInfo.GetCultureInfo("fr-FR"));

        firedProperties.ShouldContain(nameof(sut.CollapseExpandLabel));
    }
}
