using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using System.IO.Abstractions;

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
    public void when_category_tree_is_inspected_then_items_control_is_bound_to_categories()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var categoriesItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.Categories));
        categoriesItemsControl.ShouldNotBeNull("Category tree ItemsControl should be bound to the Categories collection");
    }

    [AvaloniaFact]
    public void when_loading_completes_with_no_categories_then_category_tree_is_hidden()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.IsLoading = false;

        var categoriesItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().First(ic => ReferenceEquals(ic.ItemsSource, viewModel.Categories));
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
}
