using System.Runtime.CompilerServices;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.Overview;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Logging;
using AStarDev.CloudSync.ViewModels.Overview;
using AStarDev.FunctionalParadigm;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenTheLogViewerWindow
{
    private static async Task<LogViewerViewModel> LoadedViewModelAsync(ILogEntryReader? reader = null)
    {
        var overview = new SyncOverviewViewModel(new EmptySource(), TestData.CreateLocalization(), TestData.CreateTime(), NullLogger<SyncOverviewViewModel>.Instance);
        await overview.LoadAsync(CancellationToken.None);
        var viewModel = new LogViewerViewModel(reader ?? new OneLineReader(), overview, new NoClipboard(), TestData.CreateLocalization());
        await viewModel.RefreshAsync(CancellationToken.None);

        return viewModel;
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_title_and_the_lines_are_bound_from_the_view_model()
    {
        var window = new LogViewerWindow { DataContext = await LoadedViewModelAsync() };

        window.Show();

        (window.Title, window.GetVisualDescendants().OfType<ListBox>().Single().ItemCount).ShouldBe(("Log viewer", 1));
    }

    [AvaloniaFact]
    public async Task when_a_hundred_thousand_lines_are_loaded_then_only_the_visible_rows_are_built()
    {
        var window = new LogViewerWindow { DataContext = await LoadedViewModelAsync(new ManyLinesReader(100_000)) };

        window.Show();
        window.UpdateLayout();

        var list = window.GetVisualDescendants().OfType<ListBox>().Single();
        (list.ItemCount, list.GetVisualDescendants().OfType<ListBoxItem>().Count() < 500).ShouldBe((100_000, true));
    }

    [AvaloniaFact]
    public async Task when_shown_then_every_interactive_control_has_a_screen_reader_name()
    {
        var window = new LogViewerWindow { DataContext = await LoadedViewModelAsync() };

        window.Show();

        var unnamed = window.GetVisualDescendants().OfType<Control>().Where(control => control is ComboBox or TextBox or Button or ListBox).Where(control => control.FindAncestorOfType<ComboBox>() is null && string.IsNullOrWhiteSpace(AutomationProperties.GetName(control))).ToList();
        unnamed.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public async Task when_tab_is_pressed_then_focus_reaches_every_filter_the_buttons_and_then_a_row_of_the_list_in_order()
    {
        var window = new LogViewerWindow { DataContext = await LoadedViewModelAsync() };
        window.Show();
        window.UpdateLayout();
        var visited = new List<string>();

        for (var press = 0; press < 6; press++)
        {
            window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
            visited.Add(window.FocusManager?.GetFocusedElement()?.GetType().Name ?? "none");
        }

        visited.ShouldBe(["ComboBox", "ComboBox", "TextBox", "Button", "Button", "ListBoxItem"]);
    }

    [AvaloniaFact]
    public async Task when_shown_then_the_list_sits_in_the_star_row_of_a_stretching_auto_star_grid_with_a_zero_minimum_height()
    {
        var window = new LogViewerWindow { DataContext = await LoadedViewModelAsync() };

        window.Show();

        var list = window.GetVisualDescendants().OfType<ListBox>().Single();
        var grid = list.GetVisualParent() as Grid;
        (string.Join(',', grid?.RowDefinitions.Select(row => row.Height.GridUnitType) ?? []), grid?.HorizontalAlignment, grid?.VerticalAlignment, Grid.GetRow(list), list.MinHeight, list.FindAncestorOfType<ScrollViewer>() is null)
            .ShouldBe(("Auto,Star", HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 1, 0d, true));
    }

    [AvaloniaFact]
    public async Task when_escape_is_pressed_then_the_window_stays_open_because_it_has_no_escape_behaviour()
    {
        var window = new LogViewerWindow { DataContext = await LoadedViewModelAsync() };
        window.Show();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        window.IsVisible.ShouldBeTrue();
    }

    private sealed class EmptySource : ISyncOverviewSource
    {
        public Task<Result<SyncOverview, ErrorResponse>> GetOverviewAsync(CancellationToken cancellationToken) => Task.FromResult(Result.Success<SyncOverview, ErrorResponse>(SyncOverviewFactory.Create([], [], [], [], [], 0, Option.None<DateTimeOffset>())));
    }

    private sealed class OneLineReader : ILogEntryReader
    {
        public async IAsyncEnumerable<LogLine> ReadAsync(LogQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return LogLineFactory.Create(DateTimeOffset.UnixEpoch, LogLevel.Information, Option.None<string>(), "Cat", "hello");
        }
    }

    private sealed class ManyLinesReader(int count) : ILogEntryReader
    {
        public async IAsyncEnumerable<LogLine> ReadAsync(LogQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            for (var index = 0; index < count; index++)
                yield return LogLineFactory.Create(DateTimeOffset.UnixEpoch.AddSeconds(index), LogLevel.Information, Option.None<string>(), "Cat", $"line {index}");
        }
    }

    private sealed class NoClipboard : ILogDiagnosticsClipboard
    {
        public Task SetTextAsync(string text, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
