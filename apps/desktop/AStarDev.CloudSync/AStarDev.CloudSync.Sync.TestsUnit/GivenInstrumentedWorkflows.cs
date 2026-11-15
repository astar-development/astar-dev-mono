using System.Diagnostics.Metrics;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenInstrumentedWorkflows : IDisposable
{
    private static readonly AccountId Personal = AccountIdFactory.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private readonly WorkflowTelemetry telemetry = new(new FakeTimeProvider());
    private readonly MeterListener listener = new();
    private readonly List<string> runs = [];

    public GivenInstrumentedWorkflows()
    {
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, telemetry) && instrument.Name == "cloudsync.workflow.runs")
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => runs.Add(string.Join(',', tags.ToArray().Select(tag => $"{tag.Key}={tag.Value}").Order(StringComparer.Ordinal))));
        listener.Start();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        listener.Dispose();
        telemetry.Dispose();
    }

    [Fact]
    public async Task when_a_folder_level_is_listed_then_one_folder_browse_run_is_counted_as_succeeded()
    {
        var source = new InstrumentedFolderTreeSource(new FakeFolderTreeSource(), telemetry);

        _ = await source.GetChildrenAsync(Personal, FolderTreePositionFactory.CreateRoot(), CancellationToken);

        runs.ShouldBe(["outcome=succeeded,workflow=folder_browse"]);
    }

    [Fact]
    public async Task when_a_folder_level_cannot_be_listed_then_the_failure_is_returned_and_counted_as_failed()
    {
        var failure = FolderTreeFailureFactory.CreateRulesFailed(new ErrorResponse("rules"));
        var inner = new FakeFolderTreeSource { Levels = _ => Result.Failure<IReadOnlyList<FolderTreeNode>, FolderTreeFailure>(failure) };

        var result = await new InstrumentedFolderTreeSource(inner, telemetry).GetChildrenAsync(Personal, FolderTreePositionFactory.CreateRoot(), CancellationToken);

        result.Match(_ => (object)"listed", error => error).ShouldBe(failure);
        runs.ShouldBe(["outcome=failed,workflow=folder_browse"]);
    }

    [Fact]
    public async Task when_an_account_signs_in_again_then_one_sign_in_run_is_counted_as_succeeded()
    {
        var actions = new InstrumentedAccountActions(new FakeAccountActions(), telemetry);

        _ = await actions.ReauthenticateAsync(Personal, CancellationToken);

        runs.ShouldBe(["outcome=succeeded,workflow=sign_in"]);
    }

    [Theory]
    [InlineData("cancelled", "outcome=cancelled,workflow=sign_in")]
    [InlineData("different", "outcome=failed,workflow=sign_in")]
    public async Task when_signing_in_again_does_not_complete_then_the_failure_is_returned_and_the_run_is_counted(string reason, string expectedRun)
    {
        var failure = reason == "cancelled" ? ReauthFailureFactory.CreateCancelled() : ReauthFailureFactory.CreateDifferentAccount();
        var inner = new FakeAccountActions { Reauthenticate = _ => Task.FromResult(Result.Failure<Unit, ReauthFailure>(failure)) };

        var result = await new InstrumentedAccountActions(inner, telemetry).ReauthenticateAsync(Personal, CancellationToken);

        result.Match(_ => (object)"signed-in", error => error).ShouldBe(failure);
        runs.ShouldBe([expectedRun]);
    }

    [Fact]
    public async Task when_the_other_account_commands_run_then_nothing_is_counted()
    {
        var actions = new InstrumentedAccountActions(new FakeAccountActions(), telemetry);

        _ = await actions.RemoveAsync(Personal, CancellationToken);

        runs.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("added", "outcome=succeeded,workflow=sign_in")]
    [InlineData("reauthenticated", "outcome=succeeded,workflow=sign_in")]
    [InlineData("cancelled", "outcome=cancelled,workflow=sign_in")]
    [InlineData("failed", "outcome=failed,workflow=sign_in")]
    public async Task when_an_account_is_added_then_one_sign_in_run_is_counted_for_the_outcome_and_the_outcome_is_returned(string kind, string expectedRun)
    {
        var outcome = kind switch
        {
            "added" => AccountAddOutcomeFactory.CreateAdded(Personal),
            "reauthenticated" => AccountAddOutcomeFactory.CreateReAuthenticated(Personal),
            "cancelled" => AccountAddOutcomeFactory.CreateCancelled(),
            _ => AccountAddOutcomeFactory.CreateFailed(AccountAddFailureFactory.CreateNetwork()),
        };
        var inner = new FakeAccountAdder { Add = _ => Task.FromResult(outcome) };

        var returned = await new InstrumentedAccountAdder(inner, telemetry).AddAsync(CancellationToken);

        returned.ShouldBe(outcome);
        runs.ShouldBe([expectedRun]);
    }
}
