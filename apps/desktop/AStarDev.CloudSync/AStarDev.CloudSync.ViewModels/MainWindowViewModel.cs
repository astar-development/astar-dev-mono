using System.ComponentModel;
using AStarDev.CloudSync.DisplaySettings;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.Conflicts;
using AStarDev.CloudSync.ViewModels.DisplaySettings;
using AStarDev.CloudSync.ViewModels.FolderSelection;
using AStarDev.CloudSync.ViewModels.Layouts;
using AStarDev.CloudSync.ViewModels.SyncRestart;
using AStarDev.Localization;

namespace AStarDev.CloudSync.ViewModels;

/// <summary>View model for the CloudSync main window: shows the layout chosen in the display settings and the add-account dialog over it.</summary>
public sealed class MainWindowViewModel : LocalizedObservableObject
{
    /// <summary>Creates the view model and follows layout changes in the display settings.</summary>
    /// <param name="localization">The localisation service supplying the window's text.</param>
    /// <param name="displaySettings">The layout and theme choices.</param>
    /// <param name="layoutA">Layout A, the unified account list.</param>
    /// <param name="layoutB">Layout B, grouped by provider.</param>
    /// <param name="layoutC">Layout C, sync pairs first.</param>
    /// <param name="addAccount">The add-account dialog shown over the layout.</param>
    /// <param name="accountDialog">The remove-account and sign-in dialog shown over the layout.</param>
    /// <param name="folderSelection">The folder selection dialog shown over the layout.</param>
    /// <param name="syncRestart">The question shown when Sync Now is pressed while the account is syncing.</param>
    /// <param name="conflictList">The conflict list screen shown over the layout.</param>
    public MainWindowViewModel(ILocalizationService localization, DisplaySettingsViewModel displaySettings, LayoutAViewModel layoutA, LayoutBViewModel layoutB, LayoutCViewModel layoutC, AddAccountViewModel addAccount, AccountDialogViewModel accountDialog, FolderSelectionViewModel folderSelection, ConfirmSyncRestartViewModel syncRestart, ConflictListViewModel conflictList) : base(localization)
    {
        DisplaySettings = displaySettings;
        LayoutA = layoutA;
        LayoutB = layoutB;
        LayoutC = layoutC;
        AddAccount = addAccount;
        AccountDialog = accountDialog;
        FolderSelection = folderSelection;
        SyncRestart = syncRestart;
        ConflictList = conflictList;
        syncRestart.PropertyChanged += OnSyncRestartChanged;
        conflictList.PropertyChanged += OnConflictListChanged;
        accountDialog.PropertyChanged += OnDialogChanged;
        folderSelection.PropertyChanged += OnFolderSelectionChanged;
        addAccount.PropertyChanged += OnDialogChanged;
        displaySettings.PropertyChanged += OnDisplaySettingsChanged;
    }

    /// <summary>The window title.</summary>
    public string Title => Localization.GetLocal("App.Title");

    /// <summary>The layout and theme choices.</summary>
    public DisplaySettingsViewModel DisplaySettings { get; }

    /// <summary>Layout A, the unified account list.</summary>
    public LayoutAViewModel LayoutA { get; }

    /// <summary>Layout B, grouped by provider.</summary>
    public LayoutBViewModel LayoutB { get; }

    /// <summary>Layout C, sync pairs first.</summary>
    public LayoutCViewModel LayoutC { get; }

    /// <summary>The add-account dialog shown over the layout.</summary>
    public AddAccountViewModel AddAccount { get; }

    /// <summary>The remove-account and sign-in dialog.</summary>
    public AccountDialogViewModel AccountDialog { get; }

    /// <summary>The folder selection dialog.</summary>
    public FolderSelectionViewModel FolderSelection { get; }

    /// <summary>The question shown when Sync Now is pressed while the account is syncing.</summary>
    public ConfirmSyncRestartViewModel SyncRestart { get; }

    /// <summary>The conflict list screen.</summary>
    public ConflictListViewModel ConflictList { get; }

    /// <summary>Whether a modal dialog is open, so the layout behind it is disabled.</summary>
    public bool IsDialogOpen => AddAccount.IsOpen || AccountDialog.IsOpen || FolderSelection.IsOpen || SyncRestart.IsOpen || ConflictList.IsOpen;

    /// <summary>The view model of the layout to show. Each layout keeps its own state, so switching back restores it.</summary>
    public object CurrentLayout => DisplaySettings.CurrentLayout switch
    {
        MainLayout.GroupedByProvider => LayoutB,
        MainLayout.SyncPairsFirst => LayoutC,
        _ => LayoutA
    };

    private void OnDialogChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddAccountViewModel.IsOpen))
            OnPropertyChanged(nameof(IsDialogOpen));
    }

    private void OnFolderSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FolderSelectionViewModel.IsOpen))
            OnPropertyChanged(nameof(IsDialogOpen));
    }

    private void OnConflictListChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConflictListViewModel.IsOpen))
            OnPropertyChanged(nameof(IsDialogOpen));
    }

    private void OnSyncRestartChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConfirmSyncRestartViewModel.IsOpen))
            OnPropertyChanged(nameof(IsDialogOpen));
    }

    private void OnDisplaySettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DisplaySettingsViewModel.CurrentLayout))
            OnPropertyChanged(nameof(CurrentLayout));
    }
}
