using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.Common;

/// <summary>Modal overlay keyboard behaviour: takes focus when the overlay opens, closes on Escape, keeps Tab inside it and gives focus back to the control that opened it.</summary>
/// <typeparam name="TViewModel">The overlay's view model.</typeparam>
internal sealed class OverlayBehavior<TViewModel> where TViewModel : class, INotifyPropertyChanged
{
    private readonly UserControl view;
    private readonly Func<TViewModel, bool> isOpen;
    private readonly Func<TViewModel, ICommand> dismiss;
    private readonly string? focusName;
    private TViewModel? observed;
    private bool wasOpen;
    private IInputElement? opener;

    private OverlayBehavior(UserControl view, Func<TViewModel, bool> isOpen, Func<TViewModel, ICommand> dismiss, string? focusName)
    {
        this.view = view;
        this.isOpen = isOpen;
        this.dismiss = dismiss;
        this.focusName = focusName;
    }

    internal static void Attach(UserControl view, Func<TViewModel, bool> isOpen, Func<TViewModel, ICommand> dismiss, string? focusName = null)
    {
        var behavior = new OverlayBehavior<TViewModel>(view, isOpen, dismiss, focusName);
        KeyboardNavigation.SetTabNavigation(view, KeyboardNavigationMode.Cycle);
        view.AddHandler(InputElement.KeyDownEvent, behavior.OnKeyDown);
        view.Loaded += behavior.OnLoaded;
        view.Unloaded += behavior.OnUnloaded;
        view.DataContextChanged += behavior.OnDataContextChanged;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || e.Handled || view.DataContext is not TViewModel viewModel)
            return;

        var command = dismiss(viewModel);
        if (command.CanExecute(null))
            command.Execute(null);

        e.Handled = true;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Observe(view.DataContext as TViewModel);
        FocusInitial();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) => Observe(null);

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (view.IsLoaded)
            Observe(view.DataContext as TViewModel);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        var open = observed is not null && isOpen(observed);
        if (open && !wasOpen)
        {
            opener = FocusedOutsideView();
            FocusInitial();
        }
        else if (!open && wasOpen)
            Defer(RestoreOpener);
        else if (open)
            Defer(FocusIfLost);

        wasOpen = open;
    }

    private void Observe(TViewModel? viewModel)
    {
        if (observed is not null)
            observed.PropertyChanged -= OnViewModelChanged;

        observed = viewModel;
        wasOpen = observed is not null && isOpen(observed);
        if (observed is not null)
            observed.PropertyChanged += OnViewModelChanged;
    }

    private IInputElement? FocusedOutsideView() => FocusedElement() is Visual focused && !IsInside(focused) ? (IInputElement)focused : null;

    private IInputElement? FocusedElement() => TopLevel.GetTopLevel(view)?.FocusManager?.GetFocusedElement();

    private bool IsInside(Visual visual) => ReferenceEquals(visual, view) || visual.GetVisualAncestors().Contains(view);

    private void FocusInitial() => Defer(FocusIfLost);

    private void FocusIfLost()
    {
        if (view.IsEffectivelyVisible && !(FocusedElement() is InputElement { IsEffectivelyVisible: true } focused && IsInside(focused)))
            InitialTarget()?.Focus();
    }

    private Control? InitialTarget() => focusName is null
        ? view.GetVisualDescendants().OfType<Control>().FirstOrDefault(candidate => candidate.Focusable && candidate.IsTabStop && candidate.IsEffectivelyVisible && candidate.IsEffectivelyEnabled)
        : view.FindControl<Control>(focusName);

    private void RestoreOpener()
    {
        var target = opener;
        opener = null;
        if (target is InputElement { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } element && TopLevel.GetTopLevel(element) is not null)
            element.Focus();
    }

    private static void Defer(Action action) => Dispatcher.UIThread.Post(action, DispatcherPriority.Input);
}
