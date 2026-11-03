using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.OneDriveSyncClient.Classifications;

public partial class FileClassificationsView : UserControl, IDisposable
{
    private CancellationTokenSource? cts;
    private CategoryNodeViewModel? pendingFocusNode;

    public FileClassificationsView()
    {
        InitializeComponent();
        AddHandler(GotFocusEvent, OnPreviewGotFocus, RoutingStrategies.Bubble);
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        cts?.Dispose();
        cts = null;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is not FileClassificationRulesViewModel vm)
            return;
        vm.PrepareForLoad();
        vm.VisibleCategories.CollectionChanged += OnVisibleCategoriesChanged;
        cts = new CancellationTokenSource();
        Dispatcher.UIThread.InvokeAsync(() => vm.LoadAsync(cts.Token), DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (DataContext is FileClassificationRulesViewModel vm)
            vm.VisibleCategories.CollectionChanged -= OnVisibleCategoriesChanged;
        pendingFocusNode = null;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

#pragma warning disable IDE1006 // Naming Styles - this does not apply to event handlers
    private async void OnExportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FileClassificationRulesViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        await vm.ExportAsync(topLevel.StorageProvider, cts?.Token ?? CancellationToken.None);
    }

    private async void OnImportClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FileClassificationRulesViewModel vm)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        await vm.ImportAsync(topLevel.StorageProvider, cancellationToken: cts?.Token ?? CancellationToken.None);
    }
#pragma warning restore IDE1006 // Naming Styles

    private void OnPreviewGotFocus(object? sender, FocusChangedEventArgs e) => pendingFocusNode = e.Source is TextBox { DataContext: CategoryNodeViewModel node } ? node : null;

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e) => pendingFocusNode = null;

    private void OnVisibleCategoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (pendingFocusNode is not null)
            Dispatcher.UIThread.Post(RestoreSubCategoryFocus, DispatcherPriority.Background);
    }

    private void RestoreSubCategoryFocus()
    {
        UpdateLayout();
        var target = this.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(textBox => ReferenceEquals(textBox.DataContext, pendingFocusNode));
        if (target is null)
            return;

        target.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.PageDown:
                CategoriesScrollViewer.PageDown();
                e.Handled = true;

                break;
            case Key.PageUp:
                CategoriesScrollViewer.PageUp();
                e.Handled = true;

                break;
            case Key.End:
                CategoriesScrollViewer.ScrollToEnd();
                e.Handled = true;

                break;
            case Key.Home:
                CategoriesScrollViewer.ScrollToHome();
                e.Handled = true;

                break;
        }
    }
}
