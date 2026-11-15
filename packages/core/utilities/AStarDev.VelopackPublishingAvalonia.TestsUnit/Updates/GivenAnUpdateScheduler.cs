using AStar.Dev.Velopack.Publishing.Avalonia.Updates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Velopack;

namespace AStar.Dev.Velopack.Publishing.Avalonia.TestsUnit.Updates;

public sealed class GivenAnUpdateScheduler : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly CancellationTokenSource stop = new();
    private readonly DelayRecordingTimeProvider time = new();
    private readonly ScriptedUpdates updates = new();
    private readonly InMemoryDeferralStore store = new();
    private readonly ScriptedDialog dialog = new();
    private readonly UpdateScheduler sut;

    public GivenAnUpdateScheduler()
    {
        time.SetUtcNow(Start);
        var factory = new UpdateAvailableViewModelFactory(updates, new ImmediateUpdateApplyGate(), new FakeUpdateDialogTextProvider(), NullLogger<UpdateAvailableViewModel>.Instance);
        sut = new UpdateScheduler(updates, new UpdatePolicy(), store, factory, dialog, time, NullLogger<UpdateScheduler>.Instance);
    }

    public void Dispose() => stop.Dispose();

    private static UpdateInfo CreateUpdateInfo(string version = "1.2.3")
    {
        var asset = new VelopackAsset { Version = SemanticVersion.Parse(version), NotesMarkdown = "notes" };

        return new UpdateInfo(asset, false, null, []);
    }

    private async Task<TimeSpan> RunUntilFirstWaitAsync()
    {
        var running = sut.RunAsync(stop.Token);
        var delay = await time.NextDelayAsync(TestContext.Current.CancellationToken);
        await stop.CancelAsync();
        await running;

        return delay;
    }

    [Fact]
    public async Task when_there_is_no_update_then_no_dialog_is_shown_and_it_checks_again_in_an_hour()
    {
        updates.Result = UpdateCheckResultFactory.CreateUpToDate();

        var delay = await RunUntilFirstWaitAsync();

        dialog.Shown.ShouldBeEmpty();
        delay.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task when_the_app_cannot_be_updated_then_no_dialog_is_shown()
    {
        updates.Result = UpdateCheckResultFactory.CreateNotInstalled();

        _ = await RunUntilFirstWaitAsync();

        dialog.Shown.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_an_update_is_found_then_the_user_is_asked_with_an_optional_dialog()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());

        _ = await RunUntilFirstWaitAsync();

        dialog.Shown.ShouldHaveSingleItem().ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_user_chooses_later_then_the_deferral_is_saved_and_the_user_is_asked_again_in_an_hour()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());
        dialog.Choose = viewModel => viewModel.LaterCommand.Execute(null);

        var delay = await RunUntilFirstWaitAsync();

        var deferred = store.Current.ShouldBeOfType<DeferredSince>();
        deferred.Version.ShouldBe("1.2.3");
        deferred.FirstDeferredAt.ShouldBe(Start);
        deferred.LastPromptedAt.ShouldBe(Start);
        delay.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task when_the_user_closes_the_dialog_without_choosing_then_it_counts_as_a_deferral()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());

        _ = await RunUntilFirstWaitAsync();

        store.Current.ShouldBeOfType<DeferredSince>();
    }

    [Fact]
    public async Task when_the_user_chooses_restart_now_then_no_deferral_is_saved()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());
        dialog.Choose = viewModel => viewModel.RestartNowCommand.Execute(null);

        _ = await RunUntilFirstWaitAsync();

        store.Current.ShouldBeOfType<NotDeferred>();
        updates.Applied.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_user_deferred_recently_then_they_are_not_asked_and_it_waits_for_the_rest_of_the_hour()
    {
        var info = CreateUpdateInfo();
        updates.Result = UpdateCheckResultFactory.CreateFound(info);
        store.Current = UpdateDeferralFactory.CreateDeferredSince("1.2.3", Start.AddMinutes(-20), Start.AddMinutes(-20));

        var delay = await RunUntilFirstWaitAsync();

        dialog.Shown.ShouldBeEmpty();
        delay.ShouldBe(TimeSpan.FromMinutes(40));
    }

    [Fact]
    public async Task when_the_user_deferred_over_an_hour_ago_then_they_are_asked_again_and_the_first_deferral_time_is_kept()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());
        store.Current = UpdateDeferralFactory.CreateDeferredSince("1.2.3", Start.AddHours(-30), Start.AddHours(-2));

        _ = await RunUntilFirstWaitAsync();

        dialog.Shown.ShouldHaveSingleItem().ShouldBeFalse();
        var deferred = store.Current.ShouldBeOfType<DeferredSince>();
        deferred.FirstDeferredAt.ShouldBe(Start.AddHours(-30));
        deferred.LastPromptedAt.ShouldBe(Start);
    }

    [Fact]
    public async Task when_the_update_was_deferred_for_seven_days_then_a_forced_dialog_is_shown_and_the_update_is_installed()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());
        store.Current = UpdateDeferralFactory.CreateDeferredSince("1.2.3", Start.AddDays(-7), Start.AddHours(-2));

        _ = await RunUntilFirstWaitAsync();

        dialog.Shown.ShouldHaveSingleItem().ShouldBeTrue();
        updates.Downloaded.ShouldBe(1);
        updates.Applied.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_user_restarts_from_the_forced_dialog_then_the_update_is_installed_once()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());
        store.Current = UpdateDeferralFactory.CreateDeferredSince("1.2.3", Start.AddDays(-7), Start.AddHours(-2));
        dialog.Choose = viewModel => viewModel.RestartNowCommand.Execute(null);

        _ = await RunUntilFirstWaitAsync();

        updates.Applied.ShouldBe(1);
    }

    [Fact]
    public async Task when_showing_the_dialog_fails_then_it_keeps_checking_and_waits_an_hour()
    {
        updates.Result = UpdateCheckResultFactory.CreateFound(CreateUpdateInfo());
        dialog.Choose = _ => throw new InvalidOperationException("no window");

        var delay = await RunUntilFirstWaitAsync();

        delay.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task when_an_hour_passes_then_it_checks_again()
    {
        updates.Result = UpdateCheckResultFactory.CreateUpToDate();
        var running = sut.RunAsync(stop.Token);
        _ = await time.NextDelayAsync(TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromHours(1));
        _ = await time.NextDelayAsync(TestContext.Current.CancellationToken);
        await stop.CancelAsync();
        await running;

        updates.Checks.ShouldBe(2);
    }

    [Fact]
    public async Task when_stopped_while_waiting_then_it_finishes_without_throwing()
    {
        updates.Result = UpdateCheckResultFactory.CreateUpToDate();
        var running = sut.RunAsync(stop.Token);
        _ = await time.NextDelayAsync(TestContext.Current.CancellationToken);

        await stop.CancelAsync();

        await Should.NotThrowAsync(() => running);
    }

    private sealed class DelayRecordingTimeProvider : FakeTimeProvider
    {
        private readonly System.Threading.Channels.Channel<TimeSpan> delays = System.Threading.Channels.Channel.CreateUnbounded<TimeSpan>();

        public async Task<TimeSpan> NextDelayAsync(CancellationToken cancellationToken) => await delays.Reader.ReadAsync(cancellationToken);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _ = delays.Writer.TryWrite(dueTime);

            return timer;
        }
    }

    private sealed class ScriptedUpdates : IVelopackUpdateService
    {
        public UpdateCheckResult Result { get; set; } = UpdateCheckResultFactory.CreateUpToDate();

        public int Checks { get; private set; }

        public int Downloaded { get; private set; }

        public int Applied { get; private set; }

        public bool IsInstalled => true;

        public string Channel => "test";

        public Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken cancellationToken = default) => Task.FromResult<UpdateInfo?>(null);

        public Task<UpdateCheckResult> CheckForUpdatesWithResultAsync(CancellationToken cancellationToken = default)
        {
            Checks++;

            return Task.FromResult(Result);
        }

        public Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progress = null, CancellationToken cancellationToken = default)
        {
            Downloaded++;

            return Task.CompletedTask;
        }

        public void ApplyUpdatesAndRestart(UpdateInfo updateInfo) => Applied++;
    }

    private sealed class InMemoryDeferralStore : IUpdateDeferralStore
    {
        public UpdateDeferral Current { get; set; } = UpdateDeferralFactory.CreateNotDeferred();

        public UpdateDeferral Load() => Current;

        public void Save(UpdateDeferral deferral) => Current = deferral;
    }

    private sealed class ScriptedDialog : IUpdateAvailableDialogService
    {
        public List<bool> Shown { get; } = [];

        public Action<UpdateAvailableViewModel> Choose { get; set; } = _ => { };

        public Task ShowAsync(UpdateAvailableViewModel viewModel, CancellationToken cancellationToken = default)
        {
            Shown.Add(viewModel.IsForced);
            Choose(viewModel);

            return Task.CompletedTask;
        }
    }
}
