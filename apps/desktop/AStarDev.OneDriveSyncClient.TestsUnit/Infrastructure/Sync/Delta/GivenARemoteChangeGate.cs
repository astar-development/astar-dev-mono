using System.Collections.Concurrent;
using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.OneDriveSyncClient.Accounts;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.ApplicationConfiguration;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Infrastructure.Sync.Delta;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;
using OneDriveItemId = AStar.Dev.Infrastructure.AppDb.Entities.OneDriveItemId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Sync.Delta;

public sealed class GivenARemoteChangeGate
{
    private const string StoredLink = "https://graph.microsoft.com/v1.0/drives/d/items/root/delta(token='stored')";
    private const string LatestLink = "https://graph.microsoft.com/v1.0/drives/d/items/root/delta(token='latest')";
    private const string NextLink = "https://graph.microsoft.com/v1.0/drives/d/items/root/delta(token='next')";

    private static readonly DriveId Drive = new("drive-1");
    private static readonly AccountId UserOne = new("user-1");
    private static readonly IReadOnlyList<SyncRuleEntity> Rules = [new SyncRuleEntity { AccountId = UserOne, RemotePath = "/A", RuleType = RuleType.Include, RemoteItemId = Option.Some("rule-root") }];

    private readonly IGraphService _graphService = Substitute.For<IGraphService>();
    private readonly IDriveStateRepository _driveStateRepository = Substitute.For<IDriveStateRepository>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
    private readonly ConcurrentDictionary<string, SyncedItemEntity> _syncedItems = new(StringComparer.OrdinalIgnoreCase);

    public GivenARemoteChangeGate()
    {
        _graphService.GetLatestDeltaLinkAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<CancellationToken>()).Returns(new Ok<string, string>(LatestLink));
        _syncedItems["folder-1"] = new SyncedItemEntity { AccountId = UserOne, RemoteItemId = new OneDriveItemId("folder-1") };
    }

    private RemoteChangeGate CreateSut(int maxAgeHours = 24)
        => new(_graphService, _driveStateRepository, _timeProvider, Options.Create(new SyncSettings { ProgressReportInterval = 100, FullEnumerationMaxAgeHours = maxAgeHours }), NullLogger<RemoteChangeGate>.Instance);

    private static OneDriveAccount CreateAccount() => new()
    {
        Id = UserOne,
        Profile = AccountProfileFactory.Create(string.Empty, "user@outlook.com"),
        SyncConfig = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, LocalSyncPath.Restore("/sync-root")),
        SelectedFolderIds = []
    };

    private void GivenDriveState(string? deltaLink = StoredLink, string? fingerprint = null, int lastFullHoursAgo = 1)
        => _driveStateRepository.GetByAccountIdAsync(UserOne, Arg.Any<CancellationToken>()).Returns(Option.Some(new DriveStateEntity
        {
            AccountId = UserOne,
            DeltaLink = deltaLink is null ? Option.None<string>() : Option.Some(deltaLink),
            RulesFingerprint = Option.Some(fingerprint ?? RulesFingerprintCalculator.Compute(Rules)),
            LastFullEnumerationAt = Option.Some(_timeProvider.GetUtcNow().AddHours(-lastFullHoursAgo))
        }));

    private void GivenChanges(params DeltaChange[] changes)
        => _graphService.GetDeltaChangesAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), StoredLink, Arg.Any<CancellationToken>())
            .Returns(new Ok<DeltaQueryResult, string>(DeltaQueryResultFactory.CreateChangesFound(changes, NextLink)));

    private Task<RemoteWalkDecision> Decide(RemoteChangeGate sut)
        => sut.DecideAsync(CreateAccount(), Drive, _ => Task.FromResult("token"), Rules, _syncedItems, TestContext.Current.CancellationToken);

    [Fact]
    public async Task when_there_is_no_drive_state_then_a_walk_is_required_and_the_latest_delta_link_is_captured()
    {
        _driveStateRepository.GetByAccountIdAsync(UserOne, Arg.Any<CancellationToken>()).Returns(Option.None<DriveStateEntity>());

        var decision = await Decide(CreateSut());

        var walk = decision.ShouldBeOfType<WalkRemote>();
        walk.Reason.ShouldBe(WalkReasons.NoDeltaLink);
        walk.NewDeltaLink.ShouldBe(Option.Some(LatestLink));
        await _graphService.DidNotReceive().GetDeltaChangesAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_a_walk_is_required_and_the_latest_delta_link_cannot_be_obtained_then_no_new_link_is_captured()
    {
        _driveStateRepository.GetByAccountIdAsync(UserOne, Arg.Any<CancellationToken>()).Returns(Option.None<DriveStateEntity>());
        _graphService.GetLatestDeltaLinkAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<CancellationToken>()).Returns(new Fail<string, string>("boom"));

        var decision = await Decide(CreateSut());

        decision.ShouldBeOfType<WalkRemote>().NewDeltaLink.ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_the_rules_have_changed_then_a_walk_is_required_without_querying_changes()
    {
        GivenDriveState(fingerprint: "stale");

        var decision = await Decide(CreateSut());

        decision.ShouldBeOfType<WalkRemote>().Reason.ShouldBe(WalkReasons.RulesChanged);
        await _graphService.DidNotReceive().GetDeltaChangesAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_last_full_enumeration_is_older_than_the_configured_maximum_then_a_walk_is_required()
    {
        GivenDriveState(lastFullHoursAgo: 2);

        var decision = await Decide(CreateSut(maxAgeHours: 1));

        decision.ShouldBeOfType<WalkRemote>().Reason.ShouldBe(WalkReasons.EnumerationTooOld);
    }

    [Fact]
    public async Task when_nothing_changed_remotely_then_the_walk_is_skipped_and_the_next_delta_link_is_returned()
    {
        GivenDriveState();
        GivenChanges();

        var decision = await Decide(CreateSut());

        decision.ShouldBeOfType<SkipRemote>().NewDeltaLink.ShouldBe(NextLink);
    }

    [Fact]
    public async Task when_only_unrelated_items_changed_then_the_walk_is_skipped()
    {
        GivenDriveState();
        GivenChanges(DeltaChangeFactory.CreateChanged("stranger", Option.Some("unrelated-folder")));

        var decision = await Decide(CreateSut());

        _ = decision.ShouldBeOfType<SkipRemote>();
    }

    [Fact]
    public async Task when_a_synced_item_changed_then_a_walk_is_required_and_the_next_delta_link_is_captured()
    {
        GivenDriveState();
        GivenChanges(DeltaChangeFactory.CreateChanged("new-file", Option.Some("folder-1")));

        var decision = await Decide(CreateSut());

        var walk = decision.ShouldBeOfType<WalkRemote>();
        walk.Reason.ShouldBe(WalkReasons.RelevantChanges);
        walk.NewDeltaLink.ShouldBe(Option.Some(NextLink));
    }

    [Fact]
    public async Task when_graph_says_resync_is_required_then_a_walk_is_required_with_the_latest_delta_link()
    {
        GivenDriveState();
        _graphService.GetDeltaChangesAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), StoredLink, Arg.Any<CancellationToken>())
            .Returns(new Ok<DeltaQueryResult, string>(DeltaQueryResultFactory.CreateResyncRequired()));

        var decision = await Decide(CreateSut());

        var walk = decision.ShouldBeOfType<WalkRemote>();
        walk.Reason.ShouldBe(WalkReasons.DeltaUnavailable);
        walk.NewDeltaLink.ShouldBe(Option.Some(LatestLink));
    }

    [Fact]
    public async Task when_the_delta_query_fails_then_a_walk_is_required()
    {
        GivenDriveState();
        _graphService.GetDeltaChangesAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), StoredLink, Arg.Any<CancellationToken>())
            .Returns(new Fail<DeltaQueryResult, string>("boom"));

        var decision = await Decide(CreateSut());

        decision.ShouldBeOfType<WalkRemote>().Reason.ShouldBe(WalkReasons.DeltaUnavailable);
    }
}
