using System.Windows.Input;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.ViewModels.Accounts;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Accounts;

/// <summary>The account dialog panel shown over the main window. Bind its DataContext to an <see cref="ViewModels.Accounts.AccountDialogViewModel"/>. As a modal overlay it takes focus when opened, closes on Escape, keeps Tab inside itself and returns focus to its opener.</summary>
public partial class AccountDialogView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public AccountDialogView()
    {
        InitializeComponent();
        OverlayBehavior<AccountDialogViewModel>.Attach(this, dialog => dialog.IsOpen, DismissCommand);
    }

    private static ICommand DismissCommand(AccountDialogViewModel dialog) => dialog.IsConfirmingSyncFolderChange ? dialog.CancelSyncFolderChangeCommand : dialog.CloseCommand;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
