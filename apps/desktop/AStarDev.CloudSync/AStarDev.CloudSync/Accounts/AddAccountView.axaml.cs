using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStarDev.CloudSync.Accounts;

/// <summary>The add-account dialog panel shown over the main window. Bind its DataContext to an <see cref="ViewModels.Accounts.AddAccountViewModel"/>.</summary>
public partial class AddAccountView : UserControl
{
    /// <summary>Creates the view and loads its XAML.</summary>
    public AddAccountView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
