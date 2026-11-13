using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAConflictClassifier
{
    private static readonly DateTimeOffset Moment = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);
    private static readonly ConflictBaseline Baseline = ConflictBaselineFactory.Create(new ContentHash("hash-1"), new ContentTag("tag-1"));

    private readonly ConflictClassifier classifier = new();

    [Fact]
    public void when_both_sides_changed_since_the_baseline_then_it_is_both_modified()
    {
        var evidence = Evidence(Local("hash-2"), Remote("tag-2"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateBothModified()));
    }

    [Fact]
    public void when_the_local_side_changed_and_the_cloud_deleted_it_then_it_is_local_modified_remote_deleted()
    {
        var evidence = Evidence(Local("hash-2"), SideSnapshotFactory.CreateAbsent(), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateLocalModifiedRemoteDeleted()));
    }

    [Fact]
    public void when_the_local_side_deleted_it_and_the_cloud_changed_then_it_is_local_deleted_remote_modified()
    {
        var evidence = Evidence(SideSnapshotFactory.CreateAbsent(), Remote("tag-2"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateLocalDeletedRemoteModified()));
    }

    [Fact]
    public void when_both_sides_hold_a_new_item_with_no_baseline_then_it_is_both_added()
    {
        var evidence = Evidence(Local("hash-2"), Remote("tag-2"), Option.None<ConflictBaseline>());

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateBothAdded()));
    }

    [Fact]
    public void when_a_move_target_is_in_use_then_it_is_move_target_taken_whatever_the_snapshots_say()
    {
        var evidence = ConflictEvidenceFactory.Create(Local("hash-1"), Remote("tag-1"), Option.Some(Baseline), ConflictSignalFactory.CreateMoveTargetInUse());

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateMoveTargetTaken()));
    }

    [Fact]
    public void when_a_precondition_failed_then_it_is_remote_changed_during_transfer_whatever_the_snapshots_say()
    {
        var evidence = ConflictEvidenceFactory.Create(Local("hash-2"), SideSnapshotFactory.CreateAbsent(), Option.Some(Baseline), ConflictSignalFactory.CreatePreconditionFailed());

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateRemoteChangedDuringTransfer()));
    }

    [Fact]
    public void when_only_the_local_side_changed_then_there_is_no_conflict()
    {
        var evidence = Evidence(Local("hash-2"), Remote("tag-1"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Fact]
    public void when_only_the_cloud_changed_then_there_is_no_conflict()
    {
        var evidence = Evidence(Local("hash-1"), Remote("tag-2"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Fact]
    public void when_neither_side_changed_then_there_is_no_conflict()
    {
        var evidence = Evidence(Local("hash-1"), Remote("tag-1"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Fact]
    public void when_the_local_side_deleted_it_and_the_cloud_did_not_change_then_there_is_no_conflict()
    {
        var evidence = Evidence(SideSnapshotFactory.CreateAbsent(), Remote("tag-1"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Fact]
    public void when_the_cloud_deleted_it_and_the_local_side_did_not_change_then_there_is_no_conflict()
    {
        var evidence = Evidence(Local("hash-1"), SideSnapshotFactory.CreateAbsent(), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Fact]
    public void when_both_sides_deleted_it_then_there_is_no_conflict()
    {
        var evidence = Evidence(SideSnapshotFactory.CreateAbsent(), SideSnapshotFactory.CreateAbsent(), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void when_a_never_synced_path_is_not_on_both_sides_then_there_is_no_conflict(bool localPresent, bool remotePresent)
    {
        var evidence = Evidence(localPresent ? Local("hash-2") : SideSnapshotFactory.CreateAbsent(), remotePresent ? Remote("tag-2") : SideSnapshotFactory.CreateAbsent(), Option.None<ConflictBaseline>());

        classifier.Classify(evidence).ShouldBe(Option.None<ConflictKind>());
    }

    [Fact]
    public void when_the_local_hash_is_unknown_then_the_local_side_counts_as_changed()
    {
        var unknownHash = SideSnapshotFactory.CreatePresent(10, Moment, Option.None<ContentHash>(), Option.None<ContentTag>());
        var evidence = Evidence(unknownHash, Remote("tag-2"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateBothModified()));
    }

    [Fact]
    public void when_the_cloud_tag_is_unknown_then_the_cloud_side_counts_as_changed()
    {
        var unknownTag = SideSnapshotFactory.CreatePresent(10, Moment, Option.None<ContentHash>(), Option.None<ContentTag>());
        var evidence = Evidence(Local("hash-2"), unknownTag, Baseline);

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateBothModified()));
    }

    [Fact]
    public void when_the_hash_differs_only_in_case_then_the_local_side_counts_as_changed()
    {
        var evidence = Evidence(Local("HASH-1"), Remote("tag-2"), Baseline);

        classifier.Classify(evidence).ShouldBe(Option.Some(ConflictKindFactory.CreateBothModified()));
    }

    private static SideSnapshot Local(string hash) => SideSnapshotFactory.CreatePresent(10, Moment, Option.Some(new ContentHash(hash)), Option.None<ContentTag>());

    private static SideSnapshot Remote(string tag) => SideSnapshotFactory.CreatePresent(10, Moment, Option.None<ContentHash>(), Option.Some(new ContentTag(tag)));

    private static ConflictEvidence Evidence(SideSnapshot local, SideSnapshot remote, ConflictBaseline baseline) => ConflictEvidenceFactory.Create(local, remote, Option.Some(baseline));

    private static ConflictEvidence Evidence(SideSnapshot local, SideSnapshot remote, Option<ConflictBaseline> baseline) => ConflictEvidenceFactory.Create(local, remote, baseline);
}
