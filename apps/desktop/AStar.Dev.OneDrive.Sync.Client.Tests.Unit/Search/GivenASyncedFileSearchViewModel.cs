using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Search;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Sync.Pipeline;
using AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;
using Avalonia.Headless.XUnit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Search;

public sealed class GivenASyncedFileSearchViewModel
{
    private static readonly AccountId TestAccountId = new("acc-1");
    private static readonly OneDriveItemId TestItemId = new("item-1");

    private readonly ISyncedItemRepository repository = Substitute.For<ISyncedItemRepository>();
    private readonly IFileOpenerService fileOpenerService = Substitute.For<IFileOpenerService>();
    private readonly IFileTypeClassifier fileTypeClassifier = Substitute.For<IFileTypeClassifier>();
    private readonly IAccountRepository accountRepository = Substitute.For<IAccountRepository>();
    private readonly IUiDispatcher dispatcher = new InlineUiDispatcher();
    private readonly ILocalizationService loc = Substitute.For<ILocalizationService>();

    private SyncedFileSearchViewModel CreateSut()
    {
        var vm = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);
        vm.SetActiveAccount(TestAccountId);
        return vm;
    }

    private static SyncedItemSearchResult MakeResult(string localPath = "/tmp/nonexistent_file_astar.jpg", IReadOnlyList<string>? tags = null) => new(1, TestAccountId, TestItemId, "/remote/file.jpg", localPath, DateTimeOffset.UtcNow, 1024, tags ?? []);

    [Fact]
    public async Task when_search_executes_then_criteria_name_fragment_matches_view_model_property()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.NameFragment = "report";

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.NameFragment.ShouldBe("report");
    }

    [Fact]
    public async Task when_search_executes_then_criteria_min_size_matches_view_model_property()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.MinSize = 1024;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.MinBytes.ShouldBe(1024);
    }

    [Fact]
    public async Task when_search_executes_then_criteria_max_size_matches_view_model_property()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.MaxSize = 1_048_576;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.MaxBytes.ShouldBe(1_048_576);
    }

    [Fact]
    public async Task when_search_executes_then_criteria_duplicates_only_matches_view_model_property()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.DuplicatesOnly = true;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.DuplicatesOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task when_search_returns_results_then_results_collection_is_populated()
    {
        repository.SearchAsync(Arg.Any<SyncedItemSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([MakeResult(), MakeResult()]);
        var sut = CreateSut();

        await sut.SearchCommand.ExecuteAsync(null);

        sut.Results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_search_returns_results_then_result_count_is_updated()
    {
        repository.SearchAsync(Arg.Any<SyncedItemSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([MakeResult(), MakeResult(), MakeResult()]);
        var sut = CreateSut();

        await sut.SearchCommand.ExecuteAsync(null);

        sut.ResultCount.ShouldBe(3);
    }

    [Fact]
    public void when_duplicates_only_is_true_then_show_duplicate_disclaimer_is_true()
    {
        var sut = CreateSut();
        sut.DuplicatesOnly = true;

        sut.ShowDuplicateDisclaimer.ShouldBeTrue();
    }

    [Fact]
    public void when_duplicates_only_is_false_then_show_duplicate_disclaimer_is_false()
    {
        var sut = CreateSut();
        sut.DuplicatesOnly = false;

        sut.ShowDuplicateDisclaimer.ShouldBeFalse();
    }

    [Fact]
    public void when_result_local_path_does_not_exist_then_open_file_command_is_disabled()
    {
        var result = MakeResult(localPath: "/this/path/does/not/exist/astar_test_file.jpg");
        var vm = new SyncedFileResultViewModel(result, fileTypeClassifier, fileOpenerService, dispatcher);

        vm.OpenFileCommand.CanExecute(null).ShouldBeFalse();
    }

    [Fact]
    public void when_instantiated_then_search_title_text_delegates_to_localisation_service()
    {
        loc.GetLocal("Search.Title").Returns("Search files");
        var sut = CreateSut();

        sut.SearchTitleText.ShouldBe("Search files");
    }

    [Fact]
    public void when_instantiated_then_search_button_text_delegates_to_localisation_service()
    {
        loc.GetLocal("Search.Button").Returns("Search");
        var sut = CreateSut();

        sut.SearchButtonText.ShouldBe("Search");
    }

    [Fact]
    public void when_instantiated_then_duplicate_disclaimer_text_delegates_to_localisation_service()
    {
        loc.GetLocal("Search.DuplicateDisclaimer").Returns("Showing duplicate files only.");
        var sut = CreateSut();

        sut.DuplicateDisclaimerText.ShouldBe("Showing duplicate files only.");
    }

    [Fact]
    public void when_result_is_not_local_present_then_card_opacity_is_reduced()
    {
        var result = MakeResult(localPath: "/this/path/does/not/exist/astar_test_file.jpg");
        var vm = new SyncedFileResultViewModel(result, fileTypeClassifier, fileOpenerService, dispatcher);

        vm.CardOpacity.ShouldBe(0.4);
    }

    [Fact]
    public void when_result_is_local_present_then_card_opacity_is_full()
    {
        string existingPath = Path.GetTempFileName();
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Document);
            var result = MakeResult(localPath: existingPath);
            var vm = new SyncedFileResultViewModel(result, fileTypeClassifier, fileOpenerService, dispatcher);

            vm.CardOpacity.ShouldBe(1.0);
        }
        finally
        {
            File.Delete(existingPath);
        }
    }

    [Fact]
    public void when_set_active_account_is_called_then_repository_is_not_called()
    {
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);

        sut.SetActiveAccount(TestAccountId);

        repository.DidNotReceive().GetDistinctTagNamesAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void when_set_active_account_is_called_then_available_tags_remain_empty()
    {
        repository.GetDistinctTagNamesAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns(["Image", "Video"]);
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);

        sut.SetActiveAccount(TestAccountId);

        sut.AvailableTags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_view_is_activated_then_available_tags_are_populated_from_repository()
    {
        repository.GetDistinctTagNamesAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns(["Image", "Video", "Document"]);
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);
        sut.SetActiveAccount(TestAccountId);

        await sut.OnViewActivatedAsync(CancellationToken.None);

        sut.AvailableTags.ShouldContain("Image");
        sut.AvailableTags.ShouldContain("Video");
        sut.AvailableTags.ShouldContain("Document");
    }

    [Fact]
    public async Task when_view_is_activated_with_no_active_account_then_repository_is_not_called()
    {
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);

        await sut.OnViewActivatedAsync(CancellationToken.None);

        repository.DidNotReceive().GetDistinctTagNamesAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_view_is_activated_again_with_same_tag_count_then_available_tags_collection_is_not_rebuilt()
    {
        repository.GetDistinctTagNamesAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns(["Image", "Video"]);
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);
        sut.SetActiveAccount(TestAccountId);
        await sut.OnViewActivatedAsync(CancellationToken.None);
        sut.AvailableTags.Add("ManualTag");

        await sut.OnViewActivatedAsync(CancellationToken.None);

        sut.AvailableTags.ShouldContain("ManualTag");
    }

    [Fact]
    public async Task when_view_is_activated_again_with_more_tags_then_available_tags_are_updated()
    {
        repository.GetDistinctTagNamesAsync(TestAccountId, Arg.Any<CancellationToken>())
            .Returns(["Image", "Video"], ["Image", "Video", "Document"]);
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);
        sut.SetActiveAccount(TestAccountId);

        await sut.OnViewActivatedAsync(CancellationToken.None);
        await sut.OnViewActivatedAsync(CancellationToken.None);

        sut.AvailableTags.ShouldContain("Document");
        sut.AvailableTags.Count.ShouldBe(3);
    }

    [Fact]
    public async Task when_account_changes_then_view_activation_loads_tags_for_new_account()
    {
        var secondAccountId = new AccountId("acc-2");
        repository.GetDistinctTagNamesAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns(["Image"]);
        repository.GetDistinctTagNamesAsync(secondAccountId, Arg.Any<CancellationToken>()).Returns(["Video", "Audio"]);
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);

        sut.SetActiveAccount(TestAccountId);
        await sut.OnViewActivatedAsync(CancellationToken.None);

        sut.SetActiveAccount(secondAccountId);
        await sut.OnViewActivatedAsync(CancellationToken.None);

        sut.AvailableTags.ShouldContain("Video");
        sut.AvailableTags.ShouldContain("Audio");
        sut.AvailableTags.ShouldNotContain("Image");
    }

    [Fact]
    public async Task when_account_changes_then_available_tags_are_cleared_immediately()
    {
        repository.GetDistinctTagNamesAsync(TestAccountId, Arg.Any<CancellationToken>()).Returns(["Image"]);
        var sut = new SyncedFileSearchViewModel(repository, fileOpenerService, fileTypeClassifier, accountRepository, dispatcher, loc);
        sut.SetActiveAccount(TestAccountId);
        await sut.OnViewActivatedAsync(CancellationToken.None);

        sut.SetActiveAccount(new AccountId("acc-2"));

        sut.AvailableTags.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public async Task when_search_returns_image_result_and_file_exists_then_thumbnail_is_loaded()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        await File.WriteAllBytesAsync(tmpPath, PngFixtures.OneByOnePng, TestContext.Current.CancellationToken);
        try
        {
            fileTypeClassifier.Classify(Arg.Any<string>()).Returns(FileType.Image);
            repository.SearchAsync(Arg.Any<SyncedItemSearchCriteria>(), Arg.Any<CancellationToken>()).Returns([MakeResult(tmpPath)]);
            var sut = CreateSut();

            await sut.SearchCommand.ExecuteAsync(null);
            await Task.Delay(500); // LoadThumbnailAsync is fire-and-forget from SearchAsync; allow background decode to complete

            sut.Results[0].Thumbnail.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [Fact]
    public void when_instantiated_then_selected_sort_order_index_defaults_to_zero()
    {
        var sut = CreateSut();

        sut.SelectedSortOrderIndex.ShouldBe(0);
    }

    [Fact]
    public void when_instantiated_then_available_sort_orders_has_four_entries()
    {
        var sut = CreateSut();

        sut.AvailableSortOrders.Count.ShouldBe(4);
    }

    [Fact]
    public void when_instantiated_then_sort_order_label_text_delegates_to_localisation_service()
    {
        loc.GetLocal("Search.SortOrder.Label").Returns("Sort by");
        var sut = CreateSut();

        sut.SortOrderLabelText.ShouldBe("Sort by");
    }

    [Fact]
    public async Task when_selected_sort_order_index_is_0_then_criteria_sort_order_is_name_ascending()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.SelectedSortOrderIndex = 0;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.SortOrder.ShouldBe(SearchSortOrder.NameAscending);
    }

    [Fact]
    public async Task when_selected_sort_order_index_is_1_then_criteria_sort_order_is_name_descending()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.SelectedSortOrderIndex = 1;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.SortOrder.ShouldBe(SearchSortOrder.NameDescending);
    }

    [Fact]
    public async Task when_selected_sort_order_index_is_2_then_criteria_sort_order_is_size_ascending()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.SelectedSortOrderIndex = 2;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.SortOrder.ShouldBe(SearchSortOrder.SizeAscending);
    }

    [Fact]
    public async Task when_selected_sort_order_index_is_3_then_criteria_sort_order_is_size_descending()
    {
        SyncedItemSearchCriteria? captured = null;
        repository.SearchAsync(Arg.Do<SyncedItemSearchCriteria>(c => captured = c), Arg.Any<CancellationToken>()).Returns([]);
        var sut = CreateSut();
        sut.SelectedSortOrderIndex = 3;

        await sut.SearchCommand.ExecuteAsync(null);

        captured!.SortOrder.ShouldBe(SearchSortOrder.SizeDescending);
    }
}
