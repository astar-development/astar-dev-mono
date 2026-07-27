using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenFileClassificationsViewDisplay
{
    private static FileClassificationRulesViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        return new FileClassificationRulesViewModel(Substitute.For<IFileClassificationRepository>(), Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), localization, Substitute.For<IFileSystem>());
    }

    private static FileClassificationsView CreateViewWithViewModel(FileClassificationRulesViewModel viewModel)
    {
        var view = new FileClassificationsView { DataContext = viewModel };
        view.Measure(new(1000, 800));
        view.Arrange(new(0, 0, 1000, 800));

        return view;
    }

    [AvaloniaFact]
    public void when_view_model_is_loading_then_loading_text_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var loadingBlock = sut.GetLogicalDescendants().OfType<TextBlock>().First(tb => tb.Text == "Common.Loading");
        loadingBlock.IsVisible.ShouldBeTrue("Loading text should be visible while IsLoading is true");
    }

    [AvaloniaFact]
    public void when_view_model_is_loading_then_empty_state_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var emptyStateBlock = sut.GetLogicalDescendants().OfType<TextBlock>().First(tb => tb.Text == "No classification categories defined.");
        emptyStateBlock.IsVisible.ShouldBeFalse("Empty-state text should be hidden while loading is in progress");
    }

    [AvaloniaFact]
    public void when_loading_completes_with_no_categories_then_empty_state_becomes_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.IsLoading = false;

        var emptyStateBlock = sut.GetLogicalDescendants().OfType<TextBlock>().First(tb => tb.Text == "No classification categories defined.");
        emptyStateBlock.IsVisible.ShouldBeTrue("Empty-state text should appear when loading completes with no categories");
    }

    [AvaloniaFact]
    public void when_loading_completes_with_no_categories_then_loading_text_is_hidden()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.IsLoading = false;

        var loadingBlock = sut.GetLogicalDescendants().OfType<TextBlock>().First(tb => tb.Text == "Common.Loading");
        loadingBlock.IsVisible.ShouldBeFalse("Loading text should hide once loading completes");
    }

    [AvaloniaFact]
    public void when_category_tree_is_inspected_then_items_control_is_bound_to_visible_categories()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var categoriesItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.VisibleCategories));
        categoriesItemsControl.ShouldNotBeNull("Category tree ItemsControl should be bound to the flattened VisibleCategories collection");
    }

    [AvaloniaFact]
    public void when_category_tree_is_inspected_then_items_control_uses_a_virtualizing_stack_panel()
    {
        var viewModel = CreateViewModel();
        var view = new FileClassificationsView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 1000, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        var categoriesItemsControl = view.GetLogicalDescendants().OfType<ItemsControl>().First(ic => ReferenceEquals(ic.ItemsSource, viewModel.VisibleCategories));
        categoriesItemsControl.ItemsPanelRoot.ShouldBeOfType<VirtualizingStackPanel>("Category tree must virtualize so only on-screen rows are realized");
    }

    [AvaloniaFact]
    public void when_loading_completes_with_no_categories_then_category_tree_is_hidden()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.IsLoading = false;

        var categoriesItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().First(ic => ReferenceEquals(ic.ItemsSource, viewModel.VisibleCategories));
        categoriesItemsControl.IsVisible.ShouldBeFalse("Category tree should be hidden when HasNoCategories is true");
    }

    [AvaloniaFact]
    public void when_view_is_rendered_then_add_category_button_is_bound_to_add_category_command()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var addButton = sut.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Command == viewModel.AddCategoryCommand);
        addButton.ShouldNotBeNull("Add-category button should be bound to AddCategoryCommand");
    }

    [AvaloniaFact]
    public void when_category_tree_is_inspected_then_items_control_max_width_is_740()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var categoriesItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().First(ic => ReferenceEquals(ic.ItemsSource, viewModel.VisibleCategories));
        categoriesItemsControl.MaxWidth.ShouldBe(740);
    }

    [AvaloniaFact]
    public void when_edit_clicked_then_scroll_offset_is_preserved()
    {
        var repository = Substitute.For<IFileClassificationRepository>();
        IReadOnlyList<FileClassificationCategory> categories = [.. Enumerable.Range(0, 50).Select(index => new FileClassificationCategory(new FileClassificationCategoryId(index), $"Category {index}", 1, false, false, Option.None<FileClassificationCategoryId>(), false))];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(categories));
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        var viewModel = new FileClassificationRulesViewModel(repository, Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), localization, Substitute.For<IFileSystem>());
        var view = new FileClassificationsView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 800, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var scrollViewer = view.FindControl<ScrollViewer>("CategoriesScrollViewer")!;
        var editButton = view.GetLogicalDescendants().OfType<Button>().First(button => button.Content as string == "Edit" && button.IsVisible);
        scrollViewer.Offset = new Vector(0, 50);
        window.UpdateLayout();
        double offsetBeforeEdit = scrollViewer.Offset.Y;

        editButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        scrollViewer.Offset.Y.ShouldBe(offsetBeforeEdit);
    }

    [AvaloniaFact]
    public void when_edit_clicked_then_scroll_offset_is_preserved_across_multiple_layout_passes()
    {
        var repository = Substitute.For<IFileClassificationRepository>();
        IReadOnlyList<FileClassificationCategory> categories = [.. Enumerable.Range(0, 50).Select(index => new FileClassificationCategory(new FileClassificationCategoryId(index), $"Category {index}", 1, false, false, Option.None<FileClassificationCategoryId>(), false))];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(categories));
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        var viewModel = new FileClassificationRulesViewModel(repository, Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), localization, Substitute.For<IFileSystem>());
        var view = new FileClassificationsView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 800, Height = 400 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var scrollViewer = view.FindControl<ScrollViewer>("CategoriesScrollViewer")!;
        var editButton = view.GetLogicalDescendants().OfType<Button>().First(button => button.Content as string == "Edit" && button.IsVisible);
        scrollViewer.Offset = new Vector(0, 50);
        window.UpdateLayout();
        double offsetBeforeEdit = scrollViewer.Offset.Y;

        editButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        for (var layoutPass = 0; layoutPass < 4; layoutPass++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            scrollViewer.Offset.Y.ShouldBe(offsetBeforeEdit, $"offset should hold steady on layout pass {layoutPass}");
        }
    }
}
