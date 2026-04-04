using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

/// <summary>Modal Yes/No confirmation dialog (S012 — Dialogs via Avalonia dialog API).</summary>
public partial class ConfirmationDialog : Window
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(Message));

    public ConfirmationDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    /// <summary>The body text displayed in the dialog.</summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    private void OnConfirm_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
