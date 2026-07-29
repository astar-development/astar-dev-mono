using AStar.Dev.FunctionalParadigm;
using AStar.Dev.OneDrive.Sync.Client.Classifications;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Classifications;

public sealed class GivenACategoryNodeViewModel
{
    private readonly IFileClassificationRepository repository;
    private readonly ICategoryEditDialogService categoryEditDialogService;

    public GivenACategoryNodeViewModel()
    {
        repository = Substitute.For<IFileClassificationRepository>();
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Ok<FileClassificationCategoryId, string>(new FileClassificationCategoryId(42))));
        repository.ReparentCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<Option<FileClassificationCategoryId>>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Ok<FileClassificationCategoryId, string>(new FileClassificationCategoryId(1))));
        repository.UpdateCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Ok<FileClassificationCategoryId, string>(new FileClassificationCategoryId(1))));
        categoryEditDialogService = Substitute.For<ICategoryEditDialogService>();
        categoryEditDialogService.ShowAsync(Arg.Any<CategoryNodeViewModel>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
    }

    private CategoryNodeViewModel CreateSut(int level = 1, bool includeInSearch = false, IReadOnlyList<CategoryNodeViewModel>? allCategories = null, string ancestorPath = "") =>
        new(new FileClassificationCategoryId(1), "Media", level, false, false, Option.None<FileClassificationCategoryId>(), includeInSearch, repository, categoryEditDialogService, _ => { }, allCategories ?? [], () => Task.CompletedTask, ancestorPath);

    [Fact]
    public async Task when_add_child_category_command_executed_then_category_persisted_and_child_added()
    {
        var sut = CreateSut();
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        await repository.Received(1).AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
        sut.Children.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_add_child_category_command_executed_then_new_child_name_cleared()
    {
        var sut = CreateSut();
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        sut.NewChildCategoryName.ShouldBeEmpty();
    }

    [Fact]
    public void when_level_is_3_then_add_child_category_command_still_enabled()
    {
        var sut = CreateSut(level: 3);
        sut.NewChildCategoryName = "Deep";

        sut.AddChildCategoryCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public async Task when_delete_self_command_executed_then_on_delete_self_callback_invoked()
    {
        bool callbackInvoked = false;
        CategoryNodeViewModel sut = new(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => callbackInvoked = true, [], () => Task.CompletedTask);

        await sut.DeleteSelfCommand.ExecuteAsync(null);

        callbackInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_cancel_command_executed_then_include_in_search_reverted_to_original_value()
    {
        var sut = CreateSut(includeInSearch: true);
        await sut.EditCommand.ExecuteAsync(null);
        sut.IncludeInSearch = false;

        sut.CancelCommand.Execute(null);

        sut.IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_add_child_category_command_executed_then_child_inherits_include_in_search()
    {
        var sut = CreateSut(includeInSearch: true);
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        sut.Children[0].IncludeInSearch.ShouldBeTrue();
    }

    [Fact]
    public async Task when_add_child_category_command_executed_then_child_ancestor_path_is_root_name()
    {
        var sut = CreateSut();
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        sut.Children[0].AncestorPath.ShouldBe("Media");
    }

    [Fact]
    public async Task when_add_child_category_command_executed_on_a_child_then_grandchild_ancestor_path_includes_full_chain()
    {
        var sut = CreateSut(level: 2, ancestorPath: "Root");
        sut.NewChildCategoryName = "Photos";

        await sut.AddChildCategoryCommand.ExecuteAsync(null);

        sut.Children[0].AncestorPath.ShouldBe("Root > Media");
    }

    [Fact]
    public void when_ancestor_path_is_empty_then_has_ancestor_path_is_false()
    {
        var sut = CreateSut();

        sut.HasAncestorPath.ShouldBeFalse();
    }

    [Fact]
    public void when_ancestor_path_is_not_empty_then_has_ancestor_path_is_true()
    {
        var sut = CreateSut(ancestorPath: "Media");

        sut.HasAncestorPath.ShouldBeTrue();
    }

    [Fact]
    public async Task when_edit_command_executed_then_parent_option_names_has_root_option_first()
    {
        var sut = CreateSut();

        await sut.EditCommand.ExecuteAsync(null);

        sut.ParentOptionNames[0].ShouldBe("(No parent - root)");
    }

    [Fact]
    public async Task when_edit_command_executed_then_parent_option_names_excludes_self_and_descendants()
    {
        List<CategoryNodeViewModel> allCategories = [];
        var sut = new CategoryNodeViewModel(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => { }, allCategories, () => Task.CompletedTask);
        var photos = new CategoryNodeViewModel(new FileClassificationCategoryId(3), "Photos", 2, false, false, Option.Some(sut.CategoryId), false, repository, categoryEditDialogService, _ => { }, allCategories, () => Task.CompletedTask);
        var documents = new CategoryNodeViewModel(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => { }, allCategories, () => Task.CompletedTask);
        sut.Children.Add(photos);
        allCategories.AddRange([sut, photos, documents]);

        await sut.EditCommand.ExecuteAsync(null);

        sut.ParentOptionNames.ShouldNotContain("Media");
        sut.ParentOptionNames.ShouldNotContain("Photos");
        sut.ParentOptionNames.ShouldContain("Documents");
    }

    [Fact]
    public async Task when_edit_command_executed_then_category_edit_dialog_service_shown_with_self()
    {
        var sut = CreateSut();

        await sut.EditCommand.ExecuteAsync(null);

        await categoryEditDialogService.Received(1).ShowAsync(sut, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_command_executed_with_parent_option_unchanged_then_reparent_not_called()
    {
        var sut = CreateSut();
        await sut.EditCommand.ExecuteAsync(null);
        sut.EditedName = "Media";

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.DidNotReceive().ReparentCategoryAsync(Arg.Any<FileClassificationCategoryId>(), Arg.Any<Option<FileClassificationCategoryId>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_command_executed_with_parent_option_changed_then_reparent_called_with_selected_parent()
    {
        List<CategoryNodeViewModel> allCategories = [];
        var sut = new CategoryNodeViewModel(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => { }, allCategories, () => Task.CompletedTask);
        var documents = new CategoryNodeViewModel(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => { }, allCategories, () => Task.CompletedTask);
        allCategories.AddRange([sut, documents]);
        await sut.EditCommand.ExecuteAsync(null);
        sut.EditedName = "Media";
        sut.SelectedParentOptionIndex = sut.ParentOptionNames.ToList().IndexOf("Documents");

        await sut.SaveCommand.ExecuteAsync(null);

        await repository.Received(1).ReparentCategoryAsync(sut.CategoryId, Arg.Is<Option<FileClassificationCategoryId>>(option => option.Equals(Option.Some(documents.CategoryId))), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_save_command_executed_with_parent_option_changed_then_reload_callback_invoked()
    {
        bool reloadInvoked = false;
        List<CategoryNodeViewModel> allCategories = [];
        var sut = new CategoryNodeViewModel(new FileClassificationCategoryId(1), "Media", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => { }, allCategories, () =>
        {
            reloadInvoked = true;

            return Task.CompletedTask;
        });
        var documents = new CategoryNodeViewModel(new FileClassificationCategoryId(2), "Documents", 1, false, false, Option.None<FileClassificationCategoryId>(), false, repository, categoryEditDialogService, _ => { }, allCategories, () => Task.CompletedTask);
        allCategories.AddRange([sut, documents]);
        await sut.EditCommand.ExecuteAsync(null);
        sut.EditedName = "Media";
        sut.SelectedParentOptionIndex = sut.ParentOptionNames.ToList().IndexOf("Documents");

        await sut.SaveCommand.ExecuteAsync(null);

        reloadInvoked.ShouldBeTrue();
    }

    [Fact]
    public void when_level_is_1_then_is_expanded_defaults_to_true()
    {
        var sut = CreateSut(level: 1);

        sut.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void when_level_is_2_then_is_expanded_defaults_to_false()
    {
        var sut = CreateSut(level: 2);

        sut.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void when_toggle_expanded_command_executed_then_is_expanded_flips()
    {
        var sut = CreateSut(level: 2);

        sut.ToggleExpandedCommand.Execute(null);

        sut.IsExpanded.ShouldBeTrue();
    }
}
