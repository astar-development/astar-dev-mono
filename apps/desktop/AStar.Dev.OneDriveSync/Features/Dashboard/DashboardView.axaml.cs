using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;

namespace AStar.Dev.OneDriveSync.Features.Dashboard;

public partial class DashboardView : UserControl, IDisposable
{
    private CancellationTokenSource? _loadCts;

    public DashboardView() => InitializeComponent();

    public void Dispose()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        GC.SuppressFinalize(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _loadCts = new CancellationTokenSource();

        if (DataContext is DashboardViewModel vm)
            _ = vm.LoadAsync(_loadCts.Token);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }
}
