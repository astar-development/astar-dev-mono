using AStar.Dev.FunctionalParadigm;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Avalonia.Platform.Storage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenAFileClassificationRulesViewModel
{
    private readonly IFileClassificationRepository repository;
    private readonly IFileClassificationExportImportService exportImportService;
    private readonly IFilePickerService filePickerService;
    private readonly IConfirmationDialogService confirmationDialogService;
    private readonly ICategoryEditDialogService categoryEditDialogService;
    private readonly ILocalizationService localizationService;
    private readonly IFileSystem fileSystem;

    public GivenAFileClassificationRulesViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        exportImportService = Substitute.For<IFileClassificationExportImportService>();
        filePickerService = Substitute.For<IFilePickerService>();
        confirmationDialogService = Substitute.For<IConfirmationDialogService>();
        categoryEditDialogService = Substitute.For<ICategoryEditDialogService>();
        categoryEditDialogService.ShowAsync(Arg.Any<CategoryNodeViewModel>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        localizationService = Substitute.For<ILocalizationService>();
        fileSystem = new MockFileSystem();

        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Ok<FileClassificationCategoryId, string>(new FileClassificationCategoryId(1))));
    }

    [Fact]
    public async Task when_load_async_called_then_level1_categories_populated()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_load_async_called_then_child_categories_nested_under_parent()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories.Count.ShouldBe(1);
        sut.Categories[0].Children.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_category_persisted_and_added()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        await repository.Received(1).AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
        sut.Categories.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_new_category_name_cleared()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        sut.NewCategoryName.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_category_name_empty_then_add_category_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem)
        {
            NewCategoryName = string.Empty
        };

        sut.AddCategoryCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task when_no_categories_loaded_then_has_no_categories_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeTrue();
    }

    [Fact]
    public async Task when_categories_present_then_has_no_categories_is_false()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public async Task when_import_command_invoked_and_confirmed_then_load_async_called_after_import()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        const string importFilePath = "/some/file.json";
        filePickerService.PickOpenFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(importFilePath));
        confirmationDialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                                 .Returns(Task.FromResult(true));
        exportImportService.ImportAsync(Arg.Any<IFileInfo>(), Arg.Any<CancellationToken>())
                           .Returns(Task.CompletedTask);
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.ImportAsync(storageProvider, CancellationToken.None);

        await exportImportService.Received(1).ImportAsync(Arg.Any<IFileInfo>(), Arg.Any<CancellationToken>());
        await repository.Received(1).GetAllCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_export_command_invoked_and_file_picker_returns_null_then_export_not_called()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        filePickerService.PickSaveFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(null));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.ExportAsync(storageProvider, CancellationToken.None);

        await exportImportService.DidNotReceive().ExportAsync(Arg.Any<IFileInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_export_command_invoked_with_valid_path_then_export_service_called()
    {
        var storageProvider = Substitute.For<IStorageProvider>();
        const string exportFilePath = "/some/export.json";
        filePickerService.PickSaveFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(exportFilePath));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.ExportAsync(storageProvider, CancellationToken.None);

        await exportImportService.Received(1).ExportAsync(Arg.Any<IFileInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_view_model_constructed_then_is_loading_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void when_view_model_constructed_then_is_loaded_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.IsLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_load_async_completes_then_is_loaded_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.IsLoaded.ShouldBeTrue();
    }

    [Fact]
    public async Task when_load_async_cancelled_then_is_loaded_is_false()
    {
        using var cts = new CancellationTokenSource();
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(callInfo => Task.FromCanceled<IReadOnlyList<FileClassificationCategory>>(callInfo.Arg<CancellationToken>()));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.LoadAsync(cts.Token));

        sut.IsLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_load_async_completes_then_is_loading_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void when_is_loading_is_true_then_has_no_categories_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.IsLoading.ShouldBeTrue();
        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public async Task when_load_async_completes_with_no_categories_then_has_no_categories_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeTrue();
    }

    [Fact]
    public async Task when_load_async_completes_with_categories_then_has_no_categories_is_false()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public void when_prepare_for_load_called_then_is_loading_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.PrepareForLoad();

        sut.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void when_prepare_for_load_called_then_is_loaded_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.PrepareForLoad();

        sut.IsLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_prepare_for_load_called_after_load_then_categories_are_cleared()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.PrepareForLoad();

        sut.Categories.Count.ShouldBe(0);
    }

    [Fact]
    public void when_loading_text_requested_then_returns_value_from_localization_service()
    {
        const string expectedText = "Loading...";
        localizationService.GetLocal("Common.Loading").Returns(expectedText);
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.LoadingText.ShouldBe(expectedText);
    }

    [Fact]
    public async Task when_load_async_called_with_categories_at_same_level_then_categories_ordered_alphabetically_by_name()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Zebra", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(2), "Alpha", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(3), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Name.ShouldBe("Alpha");
        sut.Categories[1].Name.ShouldBe("Media");
        sut.Categories[2].Name.ShouldBe("Zebra");
    }

    [Fact]
    public async Task when_load_async_called_with_children_at_same_level_then_children_ordered_alphabetically_by_name()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false),
            new(new FileClassificationCategoryId(2), "Videos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), false),
            new(new FileClassificationCategoryId(3), "Audio", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), false),
            new(new FileClassificationCategoryId(4), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Children[0].Name.ShouldBe("Audio");
        sut.Categories[0].Children[1].Name.ShouldBe("Photos");
        sut.Categories[0].Children[2].Name.ShouldBe("Videos");
    }

    [Fact]
    public async Task when_load_async_called_then_visible_categories_is_flattened_preorder_tree_down_to_level_two()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true),
            new(new FileClassificationCategoryId(3), "Holiday", 3, false, false, Option.Some(new FileClassificationCategoryId(2)), true),
            new(new FileClassificationCategoryId(4), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents", "Media", "Photos"]);
    }

    [Fact]
    public async Task when_level_two_node_expanded_then_its_level_three_children_become_visible()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true),
            new(new FileClassificationCategoryId(3), "Holiday", 3, false, false, Option.Some(new FileClassificationCategoryId(2)), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        var photos = sut.VisibleCategories.Single(node => node.Name == "Photos");

        photos.ToggleExpandedCommand.Execute(null);

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Media", "Photos", "Holiday"]);
    }

    [Fact]
    public async Task when_root_node_collapsed_then_its_children_become_hidden()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        var media = sut.VisibleCategories.Single(node => node.Name == "Media");

        media.ToggleExpandedCommand.Execute(null);

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Media"]);
    }

    [Fact]
    public async Task when_filter_text_matches_a_collapsed_descendant_then_it_becomes_visible()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true),
            new(new FileClassificationCategoryId(3), "Holiday", 3, false, false, Option.Some(new FileClassificationCategoryId(2)), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.FilterText = "Holiday";

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Media", "Photos", "Holiday"]);
    }

    [Fact]
    public async Task when_load_async_called_then_root_category_ancestor_path_is_empty()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].AncestorPath.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_load_async_called_then_child_category_ancestor_path_is_parent_name()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Children[0].AncestorPath.ShouldBe("Media");
    }

    [Fact]
    public async Task when_load_async_called_then_grandchild_category_ancestor_path_is_full_ancestor_chain()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true),
            new(new FileClassificationCategoryId(3), "Holiday", 3, false, false, Option.Some(new FileClassificationCategoryId(2)), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Children[0].Children[0].AncestorPath.ShouldBe("Media > Photos");
    }

    [Fact]
    public async Task when_load_async_called_then_ancestor_path_is_populated_regardless_of_ancestor_filter_state()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Archived", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        sut.ShowOnlyIncludedInSearch = false;

        sut.VisibleCategories.Single(node => node.Name == "Archived").AncestorPath.ShouldBe("Media");
    }

    [Fact]
    public async Task when_child_category_added_then_visible_categories_includes_it_in_tree_order()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Ok<FileClassificationCategoryId, string>(new FileClassificationCategoryId(3))));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        var media = sut.Categories.Single(category => category.Name == "Media");
        media.NewChildCategoryName = "Photos";

        await media.AddChildCategoryCommand.ExecuteAsync(null);

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents", "Media", "Photos"]);
    }

    [Fact]
    public async Task when_load_async_called_then_include_in_search_populated_from_category()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_add_category_command_executed_then_include_in_search_passed_to_new_category()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem)
        {
            NewCategoryName = "Media",
            IncludeInSearch = true
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        sut.Categories[0].IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_root_category_deleted_then_visible_categories_no_longer_includes_it()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.DeleteCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.CompletedTask);
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        var media = sut.Categories.Single(category => category.Name == "Media");

        await media.DeleteSelfCommand.ExecuteAsync(null);

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents"]);
    }

    [Fact]
    public void when_view_model_created_then_show_only_included_in_search_defaults_to_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.ShowOnlyIncludedInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_show_only_included_in_search_is_true_then_visible_categories_excludes_others()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.ShowOnlyIncludedInSearch = true;

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Media"]);
    }

    [Fact]
    public async Task when_show_only_included_in_search_is_toggled_off_then_visible_categories_shows_excluded_only()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        sut.ShowOnlyIncludedInSearch = true;

        sut.ShowOnlyIncludedInSearch = false;

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents"]);
    }

    [Fact]
    public async Task when_show_only_included_in_search_is_true_then_label_reflects_included()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.ShowOnlyIncludedInSearch = true;

        sut.SearchFilterLabel.ShouldBe("Showing: included in search");
    }

    [Fact]
    public async Task when_show_only_included_in_search_is_false_then_label_reflects_excluded()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.ShowOnlyIncludedInSearch = false;

        sut.SearchFilterLabel.ShouldBe("Showing: excluded from search");
    }

    [Fact]
    public void when_view_model_created_then_filter_text_defaults_to_empty()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);

        sut.FilterText.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_filter_text_set_then_visible_categories_only_includes_matching_names()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.FilterText = "doc";

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents"]);
    }

    [Fact]
    public async Task when_filter_text_matches_only_a_descendant_then_ancestor_stays_visible()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Photos", 2, false, false, Option.Some(new FileClassificationCategoryId(1)), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.FilterText = "Photos";

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Media", "Photos"]);
    }

    [Fact]
    public async Task when_filter_text_cleared_then_previous_visible_categories_restored()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), true)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);
        sut.FilterText = "doc";

        sut.FilterText = string.Empty;

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents", "Media"]);
    }

    [Fact]
    public async Task when_filter_text_matches_a_name_excluded_by_search_toggle_then_it_stays_hidden()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), true),
            new(new FileClassificationCategoryId(2), "Document Archive", 1, false, false, Option.None<FileClassificationCategoryId>(), false)
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, categoryEditDialogService, localizationService, fileSystem);
        await sut.LoadAsync(CancellationToken.None);

        sut.FilterText = "Document";

        sut.VisibleCategories.Select(node => node.Name).ShouldBe(["Documents"]);
    }
}
