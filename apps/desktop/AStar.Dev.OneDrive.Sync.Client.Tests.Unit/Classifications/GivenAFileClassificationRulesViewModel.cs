using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using Avalonia.Platform.Storage;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenAFileClassificationRulesViewModel
{
    private readonly IFileClassificationRepository repository;
    private readonly IFileClassificationExportImportService exportImportService;
    private readonly IFilePickerService filePickerService;
    private readonly IConfirmationDialogService confirmationDialogService;

    public GivenAFileClassificationRulesViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        exportImportService = Substitute.For<IFileClassificationExportImportService>();
        filePickerService = Substitute.For<IFilePickerService>();
        confirmationDialogService = Substitute.For<IConfirmationDialogService>();

        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationCategory>>([]));
        repository.GetKeywordsForCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<IReadOnlyList<FileClassificationKeywordEntry>>([]));
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Result<FileClassificationCategoryId, string>.Ok(new FileClassificationCategoryId(1))));
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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

        await sut.LoadAsync(CancellationToken.None);

        sut.Categories[0].Keywords.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_category_command_executed_then_category_persisted_and_added()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService)
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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService)
        {
            NewCategoryName = "Media"
        };

        await sut.AddCategoryCommand.ExecuteAsync(null);

        sut.NewCategoryName.ShouldBeEmpty();
    }

    [Fact]
    public void when_new_category_name_empty_then_add_category_command_disabled()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService)
        {
            NewCategoryName = string.Empty
        };

        sut.AddCategoryCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void when_no_categories_then_has_no_categories_is_true()
    {
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

        await sut.LoadAsync(CancellationToken.None);

        sut.HasNoCategories.ShouldBeFalse();
    }

    [Fact]
    public async Task when_import_command_invoked_and_file_picker_returns_null_then_delete_all_not_called()
    {
        IStorageProvider storageProvider = Substitute.For<IStorageProvider>();
        filePickerService.PickOpenFileAsync(storageProvider, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                         .Returns(Task.FromResult<string?>(null));
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

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
        FileClassificationRulesViewModel sut = new(repository, exportImportService, filePickerService, confirmationDialogService);

        await sut.ExportAsync(storageProvider, CancellationToken.None);

        await exportImportService.Received(1).ExportAsync(exportFilePath, Arg.Any<CancellationToken>());
    }
}
