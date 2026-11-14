using System.ComponentModel;
using AStarDev.CloudSync.ViewModels.Conflicts;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace AStarDev.CloudSync.Conflicts;

/// <summary>The conflict list screen. Bind its DataContext to <c>ConflictListViewModel</c>. As a modal overlay it takes focus when it is shown or opened, closes on Escape and keeps Tab inside itself.</summary>
public partial class ConflictListView : UserControl
{
    private ConflictListViewModel? observed;

    /// <summary>Creates the control and loads its XAML.</summary>
    public ConflictListView() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key != Key.Escape || e.Handled || DataContext is not ConflictListViewModel list)
            return;

        list.CloseCommand.Execute(null);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Observe(DataContext as ConflictListViewModel);
        FocusFilter();
    }

    /// <inheritdoc />
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        Observe(null);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (IsLoaded)
            Observe(DataContext as ConflictListViewModel);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConflictListViewModel.IsOpen) && observed?.IsOpen == true)
            FocusFilter();
    }

    private void Observe(ConflictListViewModel? list)
    {
        if (observed is not null)
            observed.PropertyChanged -= OnListPropertyChanged;

        observed = list;
        if (observed is not null)
            observed.PropertyChanged += OnListPropertyChanged;
    }

    private void FocusFilter() => Dispatcher.UIThread.Post(() =>
    {
        if (IsEffectivelyVisible)
            this.FindControl<ComboBox>("FilterBox")?.Focus();
    }, DispatcherPriority.Input);
}
