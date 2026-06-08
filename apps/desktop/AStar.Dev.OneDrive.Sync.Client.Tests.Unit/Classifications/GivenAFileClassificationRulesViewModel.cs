using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
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
    private readonly ILocalizationService localizationService;

    public GivenAFileClassificationRulesViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        exportImportService = Substitute.For<IFileClassificationExportImportService>();
        filePickerService = Substitute.For<IFilePickerService>();
        confirmationDialogService = Substitute.For<IConfirmationDialogService>();
        localizationService = Substitute.For<ILocalizationService>();

        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        repository.GetKeywordsForCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationKeywordEntry>>([]));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(1))));
        repository.AddKeywordAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(1)));
    }

    [Fact]
    public async Task when_load_async_called_then_level1_categories_populated()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>()),
            new(new FileClassificationCategoryId(2), "Documents", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_load_async_called_then_child_categories_nested_under_parent()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>()),
            new(new FileClassificationCategoryId(2), "Photos", 2, Option.Some(new FileClassificationCategoryId(1)))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories.Count.ShouldBe(1);
        sut.Categories[0].Children.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_load_async_called_then_keywords_loaded_for_leaf_categories()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>())
        ];
        IReadOnlyList<FileClassificationKeywordEntry> keywords =
        [
            new FileClassificationKeywordEntry(1, new FileClassificationKeyword("cats", Option.None<bool>()))
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        repository.GetKeywordsForCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(keywords));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Keywords.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_category_persisted_and_added()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService)
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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        sut.NewCategoryName.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_category_name_empty_then_add_category_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService)
        {
            NewCategoryName = string.Empty
        };

        sut.AddCategoryCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public async Task when_no_categories_loaded_then_has_no_categories_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeTrue();
    }

    [Fact]
    public async Task when_categories_present_then_has_no_categories_is_false()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public async Task when_import_command_invoked_and_file_picker_returns_null_then_delete_all_not_called()
    {
        IStorageProvider storageProvider = Substitute.For<IStorageProvider>();
        filePickerService.PickOpenFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(null));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.ImportAsync(storageProvider, CancellationToken.None);

        await repository.DidNotReceive().DeleteAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_import_command_invoked_and_confirmation_declined_then_delete_all_not_called()
    {
        IStorageProvider storageProvider = Substitute.For<IStorageProvider>();
        filePickerService.PickOpenFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>("/some/file.json"));
        confirmationDialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                                 .Returns(Task.FromResult(false));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.ImportAsync(storageProvider, CancellationToken.None);

        await repository.DidNotReceive().DeleteAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_import_command_invoked_and_confirmed_then_load_async_called_after_import()
    {
        IStorageProvider storageProvider = Substitute.For<IStorageProvider>();
        const string importFilePath = "/some/file.json";
        filePickerService.PickOpenFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(importFilePath));
        confirmationDialogService.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                                 .Returns(Task.FromResult(true));
        exportImportService.ImportAsync(importFilePath, Arg.Any<CancellationToken>())
                           .Returns(Task.CompletedTask);
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.ImportAsync(storageProvider, CancellationToken.None);

        await exportImportService.Received(1).ImportAsync(importFilePath, Arg.Any<CancellationToken>());
        await repository.Received(1).GetAllCategoriesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_export_command_invoked_and_file_picker_returns_null_then_export_not_called()
    {
        IStorageProvider storageProvider = Substitute.For<IStorageProvider>();
        filePickerService.PickSaveFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(null));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.ExportAsync(storageProvider, CancellationToken.None);

        await exportImportService.DidNotReceive().ExportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_export_command_invoked_with_valid_path_then_export_service_called()
    {
        IStorageProvider storageProvider = Substitute.For<IStorageProvider>();
        const string exportFilePath = "/some/export.json";
        filePickerService.PickSaveFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(exportFilePath));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.ExportAsync(storageProvider, CancellationToken.None);

        await exportImportService.Received(1).ExportAsync(exportFilePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_view_model_constructed_then_is_loading_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        sut.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void when_view_model_constructed_then_is_loaded_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        sut.IsLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_load_async_completes_then_is_loaded_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.IsLoaded.ShouldBeTrue();
    }

    [Fact]
    public async Task when_load_async_cancelled_then_is_loaded_is_false()
    {
        using var cts = new CancellationTokenSource();
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(callInfo => Task.FromCanceled<IReadOnlyList<FileClassificationCategory>>(callInfo.Arg<CancellationToken>()));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.LoadAsync(cts.Token));

        sut.IsLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_load_async_completes_then_is_loading_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public void when_is_loading_is_true_then_has_no_categories_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        sut.IsLoading.ShouldBeTrue();
        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public async Task when_load_async_completes_with_no_categories_then_has_no_categories_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeTrue();
    }

    [Fact]
    public async Task when_load_async_completes_with_categories_then_has_no_categories_is_false()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public void when_prepare_for_load_called_then_is_loading_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        sut.PrepareForLoad();

        sut.IsLoading.ShouldBeTrue();
    }

    [Fact]
    public void when_prepare_for_load_called_then_is_loaded_is_false()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        sut.PrepareForLoad();

        sut.IsLoaded.ShouldBeFalse();
    }

    [Fact]
    public async Task when_prepare_for_load_called_after_load_then_categories_are_cleared()
    {
        IReadOnlyList<FileClassificationCategory> categories =
        [
            new(new FileClassificationCategoryId(1), "Media", 1, Option.None<FileClassificationCategoryId>())
        ];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult(categories));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);
        await sut.LoadAsync(CancellationToken.None);

        sut.PrepareForLoad();

        sut.Categories.Count.ShouldBe(0);
    }

    [Fact]
    public void when_loading_text_requested_then_returns_value_from_localization_service()
    {
        const string expectedText = "Loading...";
        localizationService.GetLocal("Common.Loading").Returns(expectedText);
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService);

        sut.LoadingText.ShouldBe(expectedText);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_keyword_also_persisted()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        await repository.Received(1).AddKeywordAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<FileClassificationKeyword>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_add_category_command_executed_then_new_category_has_one_keyword()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService, localizationService)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        sut.Categories[0].Keywords.Count.ShouldBe(1);
    }
}
