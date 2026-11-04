using System.Windows.Input;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.Localization;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>A provider filter chip in layout A's accounts pane, e.g. "All" or "OneDrive".</summary>
public sealed class LayoutAProviderChipViewModel : LocalizedObservableObject
{
    private readonly Func<string> label;
    private readonly Func<AccountViewModel, bool> matches;
    private bool isSelected;

    /// <summary>Creates the chip.</summary>
    /// <param name="label">Produces the chip text in the current culture.</param>
    /// <param name="matches">Whether an account passes this chip's filter.</param>
    /// <param name="select">Called with this chip when the user selects it.</param>
    /// <param name="localization">Raises text changes when the culture changes.</param>
    public LayoutAProviderChipViewModel(Func<string> label, Func<AccountViewModel, bool> matches, Action<LayoutAProviderChipViewModel> select, ILocalizationService localization) : base(localization)
    {
        this.label = label;
        this.matches = matches;
        SelectCommand = new RelayCommand(() => select(this));
    }

    /// <summary>The chip text.</summary>
    public string Label => label();

    /// <summary>Whether this chip is the active filter.</summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set => SetProperty(ref isSelected, value);
    }

    /// <summary>Makes this chip the active filter.</summary>
    public ICommand SelectCommand { get; }

    /// <summary>Whether <paramref name="account"/> passes this chip's filter.</summary>
    /// <param name="account">The account to test.</param>
    /// <returns><see langword="true"/> when the account is shown while this chip is selected.</returns>
    public bool Matches(AccountViewModel account) => matches(account);
}
