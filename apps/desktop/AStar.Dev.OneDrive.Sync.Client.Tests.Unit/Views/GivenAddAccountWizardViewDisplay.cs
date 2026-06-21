using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AStar.Dev.OneDrive.Sync.Client.Onboarding;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Views;

public sealed class GivenAddAccountWizardViewDisplay
{
    private static AddAccountWizardViewModel CreateViewModel()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetLocal(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localization.GetLocal(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.Arg<string>());

        var authService = Substitute.For<IAuthService>();
        var graphService = Substitute.For<IGraphService>();

        return new AddAccountWizardViewModel(authService, graphService, localization);
    }

    private static AddAccountWizardView CreateViewWithViewModel(AddAccountWizardViewModel viewModel)
    {
        var view = new AddAccountWizardView { DataContext = viewModel };
        view.Measure(new(800, 600));
        view.Arrange(new(0, 0, 800, 600));

        return view;
    }

    [AvaloniaFact]
    public void when_wizard_is_on_sign_in_step_then_sign_in_panel_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var signInPanel = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => sp.IsVisible && sp.GetLogicalDescendants().OfType<Button>().Any(b => b.Command == viewModel.OpenBrowserCommand));
        signInPanel.ShouldNotBeNull("sign-in step StackPanel should be visible when CurrentStep is SignIn");
    }

    [AvaloniaFact]
    public void when_wizard_is_on_sign_in_step_then_select_folders_panel_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var foldersPanel = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => !sp.IsVisible && sp.GetLogicalDescendants().OfType<ItemsControl>().Any(ic => ReferenceEquals(ic.ItemsSource, viewModel.Folders)));
        foldersPanel.ShouldNotBeNull("select-folders step StackPanel should be hidden when CurrentStep is SignIn");
    }

    [AvaloniaFact]
    public void when_wizard_moves_to_select_folders_step_then_folders_panel_becomes_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.CurrentStep = WizardStep.SelectFolders;

        var foldersPanel = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => sp.IsVisible && sp.GetLogicalDescendants().OfType<ItemsControl>().Any(ic => ReferenceEquals(ic.ItemsSource, viewModel.Folders)));
        foldersPanel.ShouldNotBeNull("select-folders step StackPanel should become visible when CurrentStep changes to SelectFolders");
    }

    [AvaloniaFact]
    public void when_wizard_moves_to_select_folders_step_then_sign_in_panel_becomes_hidden()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.CurrentStep = WizardStep.SelectFolders;

        var signInPanel = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => !sp.IsVisible && sp.GetLogicalDescendants().OfType<Button>().Any(b => b.Command == viewModel.OpenBrowserCommand));
        signInPanel.ShouldNotBeNull("sign-in step StackPanel should be hidden when CurrentStep changes to SelectFolders");
    }

    [AvaloniaFact]
    public void when_wizard_moves_to_confirm_step_then_confirm_panel_is_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.CurrentStep = WizardStep.Confirm;

        viewModel.IsConfirmStep.ShouldBeTrue();
        var confirmPanel = sut.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(sp => sp.IsVisible && sp.GetLogicalDescendants().OfType<TextBlock>().Any(tb => tb.Text == "Wizard.AddAccount.ReadyToConnect"));
        confirmPanel.ShouldNotBeNull("confirm step StackPanel should be visible when CurrentStep is Confirm");
    }

    [AvaloniaFact]
    public void when_wizard_is_on_sign_in_step_then_back_button_is_hidden()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var backButton = sut.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Command == viewModel.BackCommand);
        backButton.ShouldNotBeNull();
        backButton.IsVisible.ShouldBeFalse("back button must be hidden on the first step because CanGoBack is false");
    }

    [AvaloniaFact]
    public void when_wizard_moves_to_select_folders_step_then_back_button_becomes_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.CurrentStep = WizardStep.SelectFolders;

        var backButton = sut.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Command == viewModel.BackCommand);
        backButton.ShouldNotBeNull();
        backButton.IsVisible.ShouldBeTrue("back button must become visible once CanGoBack is true");
    }

    [AvaloniaFact]
    public void when_is_waiting_for_auth_is_true_then_waiting_status_text_block_is_visible()
    {
        var viewModel = CreateViewModel();
        viewModel.SignInStatusText = "waiting...";
        viewModel.IsWaitingForAuth = true;

        var sut = CreateViewWithViewModel(viewModel);

        var visibleStatusBlocks = sut.GetLogicalDescendants().OfType<TextBlock>().Where(tb => tb.IsVisible && tb.Text == "waiting...").ToList();
        visibleStatusBlocks.ShouldNotBeEmpty("at least one status TextBlock bound to SignInStatusText should be visible when IsWaitingForAuth is true");
    }

    [AvaloniaFact]
    public void when_is_signed_in_is_true_then_signed_in_status_text_block_is_visible()
    {
        var viewModel = CreateViewModel();
        viewModel.SignInStatusText = "Signed in as test@example.com";
        viewModel.IsSignedIn = true;

        var sut = CreateViewWithViewModel(viewModel);

        var visibleStatusBlocks = sut.GetLogicalDescendants().OfType<TextBlock>().Where(tb => tb.IsVisible && tb.Text == "Signed in as test@example.com").ToList();
        visibleStatusBlocks.ShouldNotBeEmpty("signed-in status TextBlock should be visible when IsSignedIn is true");
    }

    [AvaloniaFact]
    public void when_sign_in_has_error_then_error_text_block_is_visible()
    {
        var viewModel = CreateViewModel();
        viewModel.SignInStatusText = "Authentication failed.";
        viewModel.SignInHasError = true;

        var sut = CreateViewWithViewModel(viewModel);

        var errorBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.IsVisible && tb.Text == "Authentication failed.");
        errorBlock.ShouldNotBeNull("error status TextBlock should be visible when SignInHasError is true");
    }

    [AvaloniaFact]
    public void when_is_loading_folders_is_true_then_folder_list_border_is_hidden()
    {
        var viewModel = CreateViewModel();
        viewModel.CurrentStep = WizardStep.SelectFolders;
        viewModel.IsLoadingFolders = true;

        var sut = CreateViewWithViewModel(viewModel);

        var folderListBorder = sut.GetLogicalDescendants().OfType<Border>().FirstOrDefault(b => b.MaxHeight == 200);
        folderListBorder.ShouldNotBeNull();
        folderListBorder.IsVisible.ShouldBeFalse("folder-list Border (MaxHeight=200) must be hidden while IsLoadingFolders is true");
    }

    [AvaloniaFact]
    public void when_is_loading_folders_is_false_then_folder_list_border_is_visible()
    {
        var viewModel = CreateViewModel();
        viewModel.CurrentStep = WizardStep.SelectFolders;
        viewModel.IsLoadingFolders = false;

        var sut = CreateViewWithViewModel(viewModel);

        var folderListBorder = sut.GetLogicalDescendants().OfType<Border>().FirstOrDefault(b => b.MaxHeight == 200);
        folderListBorder.ShouldNotBeNull();
        folderListBorder.IsVisible.ShouldBeTrue("folder-list Border (MaxHeight=200) must be visible when IsLoadingFolders is false");
    }

    [AvaloniaFact]
    public void when_folders_step_is_active_then_folders_items_control_is_bound_to_folders_collection()
    {
        var viewModel = CreateViewModel();
        viewModel.CurrentStep = WizardStep.SelectFolders;

        var sut = CreateViewWithViewModel(viewModel);

        var foldersItemsControl = sut.GetLogicalDescendants().OfType<ItemsControl>().FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, viewModel.Folders));
        foldersItemsControl.ShouldNotBeNull("ItemsControl in the select-folders step must be bound to the Folders collection");
    }

    [AvaloniaFact]
    public void when_title_text_is_rendered_then_echoed_localization_key_appears_in_tree()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        var titleBlock = sut.GetLogicalDescendants().OfType<TextBlock>().FirstOrDefault(tb => tb.Text == "Wizard.AddAccount.Title");
        titleBlock.ShouldNotBeNull("title TextBlock must display the echoed localization key for Wizard.AddAccount.Title");
    }

    private static List<Ellipse> FindVisibleAccentEllipses(AddAccountWizardView view)
    {
        return [.. view.GetLogicalDescendants().OfType<Grid>().Where(g => g.Width == 8 && g.Height == 8).Select(g => g.Children.OfType<Ellipse>().Skip(1).First()).Where(accent => accent.IsVisible)];
    }

    [AvaloniaFact]
    public void when_wizard_is_on_sign_in_step_then_exactly_one_accent_step_indicator_ellipse_is_visible()
    {
        var viewModel = CreateViewModel();

        var sut = CreateViewWithViewModel(viewModel);

        FindVisibleAccentEllipses(sut).Count.ShouldBe(1, "exactly one step-indicator accent ellipse should be visible on the sign-in step");
    }

    [AvaloniaFact]
    public void when_wizard_moves_to_select_folders_step_then_exactly_one_accent_ellipse_remains_visible()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);

        viewModel.CurrentStep = WizardStep.SelectFolders;

        FindVisibleAccentEllipses(sut).Count.ShouldBe(1, "exactly one step-indicator accent ellipse should be visible on the select-folders step");
    }

    [AvaloniaFact]
    public void when_step_changes_after_render_then_step_flags_update()
    {
        var viewModel = CreateViewModel();
        var sut = CreateViewWithViewModel(viewModel);
        viewModel.IsSignInStep.ShouldBeTrue();

        viewModel.CurrentStep = WizardStep.Confirm;

        viewModel.IsConfirmStep.ShouldBeTrue();
        viewModel.IsSignInStep.ShouldBeFalse("IsSignInStep should be false after navigating to Confirm step");
    }
}
