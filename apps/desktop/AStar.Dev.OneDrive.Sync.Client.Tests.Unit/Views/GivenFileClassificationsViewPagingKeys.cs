using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenFileClassificationsViewPagingKeys
{
    private static (FileClassificationsView View, ScrollViewer ScrollViewer) CreateOverflowingView()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var repository = Substitute.For<IFileClassificationRepository>();
        IReadOnlyList<FileClassificationCategory> categories = Enumerable.Range(0, 30)
            .Select(categoryIndex => new FileClassificationCategory(new FileClassificationCategoryId(categoryIndex), $"Category {categoryIndex}", 1, false, false, Option.None<FileClassificationCategoryId>(), false))
            .ToList();
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(categories);

        var viewModel = new FileClassificationRulesViewModel(repository, Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), localization, Substitute.For<IFileSystem>());
        var view = new FileClassificationsView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 400, Height = 400, SizeToContent = SizeToContent.Manual };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var scrollViewer = view.GetLogicalDescendants().OfType<ScrollViewer>().First();

        return (view, scrollViewer);
    }

    [AvaloniaFact]
    public void when_page_down_key_is_raised_then_scroll_offset_moves_down()
    {
        var (view, scrollViewer) = CreateOverflowingView();

        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.PageDown });

        scrollViewer.Offset.Y.ShouldBeGreaterThan(0, "PageDown should scroll the classification list content downward");
    }

    [AvaloniaFact]
    public void when_page_down_key_is_raised_then_event_is_marked_handled()
    {
        var (view, _) = CreateOverflowingView();
        var keyEventArgs = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.PageDown };

        view.RaiseEvent(keyEventArgs);

        keyEventArgs.Handled.ShouldBeTrue("the view should consume PageDown rather than letting it fall through with no effect");
    }

    [AvaloniaFact]
    public void when_page_up_key_is_raised_after_scrolling_down_then_scroll_offset_moves_back_up()
    {
        var (view, scrollViewer) = CreateOverflowingView();
        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.PageDown });
        double offsetAfterPageDown = scrollViewer.Offset.Y;

        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.PageUp });

        scrollViewer.Offset.Y.ShouldBeLessThan(offsetAfterPageDown, "PageUp should scroll the classification list content back upward");
    }

    [AvaloniaFact]
    public void when_end_key_is_raised_then_scroll_offset_moves_to_the_extent_bottom()
    {
        var (view, scrollViewer) = CreateOverflowingView();

        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.End });

        scrollViewer.Offset.Y.ShouldBe(scrollViewer.Extent.Height - scrollViewer.Viewport.Height, "End should scroll the classification list content all the way to the bottom");
    }

    [AvaloniaFact]
    public void when_home_key_is_raised_after_scrolling_down_then_scroll_offset_moves_to_the_top()
    {
        var (view, scrollViewer) = CreateOverflowingView();
        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.End });

        view.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.Home });

        scrollViewer.Offset.Y.ShouldBe(0, "Home should scroll the classification list content back to the top");
    }

    [AvaloniaFact]
    public void when_end_key_is_raised_then_event_is_marked_handled()
    {
        var (view, _) = CreateOverflowingView();
        var keyEventArgs = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Source = view, Key = Key.End };

        view.RaiseEvent(keyEventArgs);

        keyEventArgs.Handled.ShouldBeTrue("the view should consume End rather than letting it fall through with no effect");
    }
}
