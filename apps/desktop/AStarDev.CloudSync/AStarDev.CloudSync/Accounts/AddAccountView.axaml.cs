using System.Windows.Input;
using AStarDev.CloudSync.Common;
using AStarDev.CloudSync.ViewModels.Accounts;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Accounts;

/// <summary>The add-account dialog panel shown over the main window. Bind its DataContext to an <see cref="ViewModels.Accounts.AddAccountViewModel"/>. As a modal overlay it takes focus when opened, closes on Escape, keeps Tab inside itself and returns focus to its opener.</summary>
public partial class AddAccountView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public AddAccountView()
    {
        InitializeComponent();
        OverlayBehavior<AddAccountViewModel>.Attach(this, dialog => dialog.IsOpen, DismissCommand);
    }

    private static ICommand DismissCommand(AddAccountViewModel dialog)
    {
        if (dialog.IsOfferingFolders)
            return dialog.LaterCommand;

        return dialog.IsShowingMessage ? dialog.CloseCommand : dialog.CancelCommand;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
