using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Overview;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Logging;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Logging;

public sealed class GivenALogViewerViewModel
{
    private static readonly CloudAccount Photos = AttentionItems.Account("Photos");
    private static readonly CloudAccount Work = AttentionItems.Account("Work");
    private static readonly DateTimeOffset Noon = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeLogEntryReader reader = new();
    private readonly FakeLogDiagnosticsClipboard clipboard = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string HashOf(CloudAccount account) => LogHash.Of(account.Id.Value.ToString());

    private static LogLine Line(LogLevel level, Option<string> account, string message) => LogLineFactory.Create(Noon, level, account, "Cat", message);

    private async Task<LogViewerViewModel> LoadedAsync(params CloudAccount[] accounts)
    {
        var source = new SwitchableSource(SyncOverviewFactory.Create(accounts, [], [], [], [], 0, Option.None<DateTimeOffset>()));
        var overview = new SyncOverviewViewModel(source, TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken);
        var viewModel = new LogViewerViewModel(reader, overview, clipboard, TestData.CreateLocalization());
        await viewModel.RefreshAsync(CancellationToken);

        return viewModel;
    }

    [Fact]
    public async Task when_the_files_are_read_then_every_line_is_shown_with_its_time_level_category_and_message()
    {
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "Pass finished"));

        var viewModel = await LoadedAsync();

        viewModel.Lines.ShouldBe(["2026-11-15 12:00:00.000 Information Cat: Pass finished"]);
        viewModel.StatusText.ShouldBe("1 lines");
    }

    [Fact]
    public async Task when_the_viewer_is_opened_then_the_account_choices_start_with_all_accounts_then_each_account_by_name()
    {
        var viewModel = await LoadedAsync(Photos, Work);

        viewModel.AccountOptions.Select(option => option.Label).ShouldBe(["All accounts", "Photos", "Work"]);
    }

    [Fact]
    public async Task when_an_account_is_chosen_then_only_its_lines_are_shown()
    {
        reader.Lines.Add(Line(LogLevel.Information, Option.Some(HashOf(Photos)), "photos line"));
        reader.Lines.Add(Line(LogLevel.Information, Option.Some(HashOf(Work)), "work line"));
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "app line"));
        var viewModel = await LoadedAsync(Photos, Work);

        viewModel.SelectedAccount = viewModel.AccountOptions[1];

        viewModel.Lines.ShouldHaveSingleItem().ShouldContain("photos line");
    }

    [Fact]
    public async Task when_a_level_is_chosen_then_lower_lines_are_hidden()
    {
        reader.Lines.Add(Line(LogLevel.Debug, Option.None<string>(), "debug line"));
        reader.Lines.Add(Line(LogLevel.Warning, Option.None<string>(), "warning line"));
        var viewModel = await LoadedAsync();

        viewModel.SelectedLevel = viewModel.LevelOptions.Single(option => option.Value == LogLevel.Warning);

        viewModel.Lines.ShouldHaveSingleItem().ShouldContain("warning line");
    }

    [Fact]
    public async Task when_text_is_typed_then_only_lines_holding_it_are_shown_and_the_files_are_not_read_again()
    {
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "throttled by the provider"));
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "all fine"));
        var viewModel = await LoadedAsync();

        viewModel.SearchText = "THROTTLED";

        (viewModel.Lines.Count, viewModel.StatusText, reader.Reads).ShouldBe((1, "1 lines", 1));
    }

    [Fact]
    public async Task when_the_filters_match_nothing_then_the_list_is_empty_and_says_so()
    {
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "something"));
        var viewModel = await LoadedAsync();

        viewModel.SearchText = "absent";

        (viewModel.Lines.Count, viewModel.StatusText).ShouldBe((0, "0 lines"));
    }

    [Fact]
    public async Task when_the_log_changes_and_the_viewer_reloads_then_the_new_lines_are_shown_under_the_same_filters()
    {
        var viewModel = await LoadedAsync();
        viewModel.SearchText = "new";
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "a new line"));
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "an old line"));

        await viewModel.RefreshCommand.ExecuteAsync(null);

        viewModel.Lines.ShouldHaveSingleItem().ShouldContain("a new line");
    }

    [Fact]
    public async Task when_diagnostics_are_copied_then_the_filtered_lines_go_to_the_clipboard_one_per_line()
    {
        reader.Lines.Add(Line(LogLevel.Warning, Option.None<string>(), "first"));
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "second"));
        reader.Lines.Add(Line(LogLevel.Debug, Option.None<string>(), "hidden"));
        var viewModel = await LoadedAsync();
        viewModel.SelectedLevel = viewModel.LevelOptions.Single(option => option.Value == LogLevel.Information);

        await viewModel.CopyDiagnosticsCommand.ExecuteAsync(null);

        clipboard.Copied.ShouldBe(["2026-11-15 12:00:00.000 Warning     Cat: first\n2026-11-15 12:00:00.000 Information Cat: second"]);
    }

    [Fact]
    public async Task when_the_texts_are_read_then_they_come_from_the_localisation_service()
    {
        var viewModel = await LoadedAsync();

        (viewModel.TitleText, viewModel.AccountText, viewModel.LevelText, viewModel.SearchLabelText, viewModel.RefreshText, viewModel.CopyText).ShouldBe(("Log viewer", "Account", "Level", "Find in the log", "Reload", "Copy diagnostics"));
    }

    [Fact]
    public async Task when_the_text_is_cleared_to_nothing_then_every_line_is_shown_again()
    {
        reader.Lines.Add(Line(LogLevel.Information, Option.None<string>(), "one"));
        var viewModel = await LoadedAsync();
        viewModel.SearchText = "zzz";

        viewModel.SearchText = null!;

        viewModel.Lines.Count.ShouldBe(1);
    }

    private sealed class SwitchableSource(SyncOverview initial) : ISyncOverviewSource
    {
        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(initial));
    }
}
