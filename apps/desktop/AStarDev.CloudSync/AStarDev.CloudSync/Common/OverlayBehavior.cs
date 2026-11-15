using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AStarDev.CloudSync.Common;

/// <summary>Modal overlay keyboard behaviour: takes focus when the overlay opens, closes on Escape wherever focus is while it is open, keeps Tab inside it and gives focus back to the control that opened it, or to the first usable control outside it when the opener is gone, hidden or disabled.</summary>
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
    private TopLevel? host;

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
        view.Loaded += behavior.OnLoaded;
        view.Unloaded += behavior.OnUnloaded;
        view.DataContextChanged += behavior.OnDataContextChanged;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || e.Handled || view.DataContext is not TViewModel viewModel || !isOpen(viewModel))
            return;

        var command = dismiss(viewModel);
        if (command.CanExecute(null))
            command.Execute(null);

        e.Handled = true;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        host = TopLevel.GetTopLevel(view)!;
        host.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        Observe(view.DataContext as TViewModel);
        FocusInitial();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        host!.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        host = null;
        Observe(null);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (view.IsLoaded)
            Observe(view.DataContext as TViewModel);
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnViewModelChanged(sender, e), DispatcherPriority.Input);

            return;
        }

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

    private void FocusInitial() => Defer(() =>
    {
        if (view.IsEffectivelyVisible && !FocusIsUsableInside())
            InitialTarget()?.Focus();
    });

    private void FocusIfLost()
    {
        var focused = FocusedElement();
        if (view.IsEffectivelyVisible && (focused is null || (focused is Visual visual && IsInside(visual) && !FocusIsUsableInside())))
            InitialTarget()?.Focus();
    }

    private bool FocusIsUsableInside() => FocusedElement() is InputElement { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } focused && IsInside(focused);

    private Control? InitialTarget() => focusName is null
        ? view.GetVisualDescendants().OfType<Control>().FirstOrDefault(candidate => candidate.Focusable && candidate.IsTabStop && candidate.IsEffectivelyVisible && candidate.IsEffectivelyEnabled)
        : view.FindControl<Control>(focusName) ?? view.GetVisualDescendants().OfType<Control>().FirstOrDefault(candidate => candidate.Name == focusName);

    private void RestoreOpener()
    {
        var target = opener;
        opener = null;
        if (target is InputElement { IsEffectivelyVisible: true, IsEffectivelyEnabled: true } element && TopLevel.GetTopLevel(element) is not null)
            element.Focus();
        else if (FocusIsLostOrInsideView())
            FallbackTarget()?.Focus();
    }

    private bool FocusIsLostOrInsideView() => FocusedElement() is not Visual focused || IsInside(focused);

    private InputElement? FallbackTarget() => host!.GetVisualDescendants().OfType<InputElement>().FirstOrDefault(candidate => candidate.Focusable && candidate.IsTabStop && candidate.IsEffectivelyVisible && candidate.IsEffectivelyEnabled && !IsInside(candidate));

    private static void Defer(Action action) => Dispatcher.UIThread.Post(action, DispatcherPriority.Input);
}
