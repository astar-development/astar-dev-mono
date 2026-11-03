using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Classifications;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.Localization;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Views;

[Collection("AvaloniaHeadless")]
public sealed class GivenFileClassificationsViewEnterToAdd
{
    private const string RootPlaceholder = "Classifications.CategoryNamePlaceholder";
    private const string SubPlaceholder = "Classifications.NewSubCategoryPlaceholder";

    private readonly IFileClassificationRepository repository = Substitute.For<IFileClassificationRepository>();

    private async Task<(Window Window, FileClassificationRulesViewModel ViewModel)> ShowViewAsync()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        IReadOnlyList<FileClassificationCategory> categories = [new FileClassificationCategory(new FileClassificationCategoryId(1), "Parent", 1, false, false, Option.None<FileClassificationCategoryId>(), true)];
        repository.GetAllCategoriesAsync(Arg.Any<CancellationToken>()).Returns(categories);
        repository.AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<FileClassificationCategoryId, string>>(new Ok<FileClassificationCategoryId, string>(new FileClassificationCategoryId(99))));

        var viewModel = new FileClassificationRulesViewModel(repository, Substitute.For<IFileClassificationExportImportService>(), Substitute.For<IFilePickerService>(), Substitute.For<IConfirmationDialogService>(), Substitute.For<ICategoryEditDialogService>(), localization, Substitute.For<IFileSystem>(), Substitute.For<IFileReclassificationService>());
        await viewModel.LoadAsync();
        var window = new Window { Content = new FileClassificationsView { DataContext = viewModel }, Width = 1000, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();

        return (window, viewModel);
    }

    private static TextBox FindTextBox(Window window, string placeholder) => window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.PlaceholderText == placeholder);

    private static async Task PressEnterAsync(Window window)
    {
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        await Task.Delay(200);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    [AvaloniaFact]
    public async Task when_enter_is_pressed_in_the_root_name_textbox_then_the_category_is_added()
    {
        var (window, viewModel) = await ShowViewAsync();
        var textBox = FindTextBox(window, RootPlaceholder);
        textBox.Focus();
        viewModel.NewCategoryName = "Fresh";

        await PressEnterAsync(window);

        await repository.Received(1).AddCategoryAsync(Arg.Is<FileClassificationCategory>(category => category.Name == "Fresh"), Arg.Any<CancellationToken>());
    }

    [AvaloniaFact]
    public async Task when_enter_is_pressed_in_the_root_name_textbox_then_the_textbox_keeps_focus()
    {
        var (window, viewModel) = await ShowViewAsync();
        var textBox = FindTextBox(window, RootPlaceholder);
        textBox.Focus();
        viewModel.NewCategoryName = "Fresh";

        await PressEnterAsync(window);

        await repository.Received(1).AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
        FindTextBox(window, RootPlaceholder).IsFocused.ShouldBeTrue();
    }

    [AvaloniaFact]
    public async Task when_enter_is_pressed_in_the_root_name_textbox_with_no_name_then_nothing_is_added()
    {
        var (window, _) = await ShowViewAsync();
        FindTextBox(window, RootPlaceholder).Focus();

        await PressEnterAsync(window);

        await repository.DidNotReceive().AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
    }

    [AvaloniaFact]
    public async Task when_enter_is_pressed_in_a_sub_category_textbox_then_the_child_is_added()
    {
        var (window, viewModel) = await ShowViewAsync();
        viewModel.VisibleCategories.Single().NewChildCategoryName = "Child";
        FindTextBox(window, SubPlaceholder).Focus().ShouldBeTrue();

        await PressEnterAsync(window);

        await repository.Received(1).AddCategoryAsync(Arg.Is<FileClassificationCategory>(category => category.Name == "Child"), Arg.Any<CancellationToken>());
    }

    [AvaloniaFact]
    public async Task when_enter_is_pressed_in_a_sub_category_textbox_then_a_sub_category_textbox_keeps_focus()
    {
        var (window, viewModel) = await ShowViewAsync();
        var parent = viewModel.VisibleCategories.Single();
        parent.NewChildCategoryName = "Child";
        FindTextBox(window, SubPlaceholder).Focus().ShouldBeTrue();

        await PressEnterAsync(window);

        await repository.Received(1).AddCategoryAsync(Arg.Any<FileClassificationCategory>(), Arg.Any<CancellationToken>());
        window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.PlaceholderText == SubPlaceholder && textBox.DataContext == parent).IsFocused.ShouldBeTrue();
    }
}
