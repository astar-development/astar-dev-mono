using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Accounts;

/// <summary>The remove-account and sign-in dialog panel shown over the main window. Bind its DataContext to an <see cref="ViewModels.Accounts.AccountDialogViewModel"/>.</summary>
public partial class AccountDialogView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public AccountDialogView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
