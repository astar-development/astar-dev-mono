using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.TestsUnit.Overlays;

public class GivenAnOverlayDialog
{
    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_opens_then_every_interactive_control_has_a_screen_reader_name(string key)
    {
        var (window, view, vm, overlay, _) = await ShowAsync(key);

        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();

        ScreenReaderNames.Unnamed(view).ShouldBeEmpty();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_opens_then_focus_moves_inside_it(string key)
    {
        var (window, view, vm, overlay, _) = await ShowAsync(key);

        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();

        InsideOf(window, view).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_escape_is_pressed_then_the_dialog_closes(string key)
    {
        var (window, _, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeFalse();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_focus_is_outside_the_open_dialog_and_escape_is_pressed_then_the_dialog_closes(string key)
    {
        var (window, _, vm, overlay, opener) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        opener.Focus();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeFalse();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_another_key_is_pressed_then_the_open_dialog_stays_open(string key)
    {
        var (window, _, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_escape_is_pressed_and_the_dialog_is_closed_then_nothing_happens(string key)
    {
        var (window, _, vm, overlay, _) = await ShowAsync(key);

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeFalse();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_something_else_already_handled_escape_then_the_dialog_stays_open(string key)
    {
        var (window, _, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        window.AddHandler(InputElement.KeyDownEvent, (_, e) => e.Handled = true, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_view_has_another_data_context_then_escape_does_not_close_the_dialog(string key)
    {
        var (window, view, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        view.DataContext = new object();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_is_removed_from_the_window_then_escape_no_longer_reaches_it(string key)
    {
        var (window, view, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        ((Panel)view.Parent!).Children.Remove(view);
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        overlay.IsOpen(vm).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_tab_is_pressed_on_the_last_control_then_focus_stays_inside_the_dialog(string key)
    {
        var (window, view, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        var last = view.GetVisualDescendants().OfType<InputElement>().Last(candidate => candidate.Focusable && candidate.IsTabStop && candidate.IsEffectivelyVisible && candidate.IsEffectivelyEnabled);
        last.Focus();

        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);

        InsideOf(window, view).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_shift_tab_is_pressed_on_the_first_control_then_focus_stays_inside_the_dialog(string key)
    {
        var (window, view, vm, overlay, _) = await ShowAsync(key);
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        var first = view.GetVisualDescendants().OfType<InputElement>().First(candidate => candidate.Focusable && candidate.IsTabStop && candidate.IsEffectivelyVisible && candidate.IsEffectivelyEnabled);
        first.Focus();

        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);

        InsideOf(window, view).ShouldBeTrue();
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_closes_then_focus_returns_to_the_control_that_opened_it(string key)
    {
        var (window, view, vm, overlay, opener) = await ShowAsync(key);
        opener.Focus();

        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        var focusedWhileOpen = InsideOf(window, view);
        overlay.Close(vm);
        Dispatcher.UIThread.RunJobs();

        focusedWhileOpen.ShouldBeTrue();
        window.FocusManager!.GetFocusedElement().ShouldBeSameAs(opener);
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_closes_and_the_opener_is_gone_then_closing_does_not_fail(string key)
    {
        var (window, _, vm, overlay, opener) = await ShowAsync(key);
        opener.Focus();
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        ((Panel)opener.Parent!).Children.Remove(opener);

        overlay.Close(vm);
        Dispatcher.UIThread.RunJobs();

        overlay.IsOpen(vm).ShouldBeFalse();
        window.FocusManager!.GetFocusedElement().ShouldNotBeSameAs(opener);
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_closes_and_the_opener_is_hidden_then_focus_goes_to_a_visible_control_outside_the_dialog(string key)
    {
        var (window, view, vm, overlay, opener) = await ShowAsync(key);
        opener.Focus();
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        opener.IsVisible = false;

        overlay.Close(vm);
        Dispatcher.UIThread.RunJobs();

        window.FocusManager!.GetFocusedElement().ShouldBeOfType<Button>().ShouldSatisfyAllConditions(button => button.ShouldNotBeSameAs(opener), button => button.IsEffectivelyVisible.ShouldBeTrue(), button => button.GetVisualAncestors().Contains(view).ShouldBeFalse());
    }

    [AvaloniaTheory]
    [InlineData("AddAccount")]
    [InlineData("AccountDialog")]
    [InlineData("FolderSelection")]
    [InlineData("SyncRestart")]
    [InlineData("ConflictList")]
    public async Task when_the_dialog_closes_and_the_opener_is_disabled_then_focus_goes_to_an_enabled_control_outside_the_dialog(string key)
    {
        var (window, view, vm, overlay, opener) = await ShowAsync(key);
        opener.Focus();
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        opener.IsEnabled = false;

        overlay.Close(vm);
        Dispatcher.UIThread.RunJobs();

        window.FocusManager!.GetFocusedElement().ShouldBeOfType<Button>().ShouldSatisfyAllConditions(button => button.ShouldNotBeSameAs(opener), button => button.IsEffectivelyEnabled.ShouldBeTrue(), button => button.GetVisualAncestors().Contains(view).ShouldBeFalse());
    }

    [AvaloniaFact]
    public async Task when_the_dialog_closes_and_nothing_outside_it_can_take_focus_then_closing_does_not_fail()
    {
        var (_, view, vm, overlay, opener) = await ShowAsync("ConflictList");
        opener.Focus();
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        foreach (var other in ((Panel)view.Parent!).Children.Where(child => !ReferenceEquals(child, view)))
            other.IsVisible = false;

        overlay.Close(vm);
        Dispatcher.UIThread.RunJobs();

        overlay.IsOpen(vm).ShouldBeFalse();
    }

    [AvaloniaFact]
    public async Task when_a_chained_dialog_closes_over_a_hidden_opener_then_focus_does_not_fall_onto_another_open_dialog()
    {
        var (window, _, vm, overlay, opener) = await ShowAsync("ConflictList");
        opener.Focus();
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        var other = new Button { Content = "Other" };
        ((Panel)window.Content!).Children.Add(other);
        other.Focus();
        opener.IsVisible = false;

        overlay.Close(vm);
        Dispatcher.UIThread.RunJobs();

        window.FocusManager!.GetFocusedElement().ShouldBeSameAs(other);
    }

    [AvaloniaFact]
    public async Task when_the_sync_restart_question_opens_from_another_thread_then_focus_moves_inside_it()
    {
        var (window, view, vm, _, _) = await ShowAsync("SyncRestart");

        Task.Run(() => { _ = vm.SyncRestart.AskAsync(AccountIdFactory.Create(Guid.NewGuid()), TestContext.Current.CancellationToken); }, TestContext.Current.CancellationToken).Wait(TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();

        InsideOf(window, view).ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task when_another_dialog_opens_over_an_open_one_then_the_open_one_does_not_take_focus_back()
    {
        var (window, view, vm, overlay, _) = await ShowAsync("ConflictList");
        await overlay.Open(vm);
        Dispatcher.UIThread.RunJobs();
        var other = new Button { Content = "Other" };
        ((Panel)window.Content!).Children.Add(other);
        other.Focus();

        vm.ConflictList.SelectedFilter = vm.ConflictList.Filters[^1];
        Dispatcher.UIThread.RunJobs();

        window.FocusManager!.GetFocusedElement().ShouldBeSameAs(other);
        InsideOf(window, view).ShouldBeFalse();
    }

    private static bool InsideOf(Window window, Control view) => window.FocusManager!.GetFocusedElement() is Visual focused && focused.GetVisualAncestors().Contains(view);

    private static async Task<(Window Window, Control View, MainWindowViewModel ViewModel, OverlayCase Overlay, Button Opener)> ShowAsync(string key)
    {
        var overlay = OverlayCase.For(key);
        var vm = TestData.CreateMainWindowViewModel(await TestData.CreateLoadedOverviewViewModelAsync(), TestData.CreateDisplaySettings());
        var view = overlay.CreateView();
        view.DataContext = overlay.Dialog(vm);
        var opener = new Button { Content = "Opener" };
        var window = new Window { Width = 1000, Height = 800, Content = new StackPanel { Children = { opener, view, new Button { Content = "Behind" } } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        return (window, view, vm, overlay, opener);
    }
}
