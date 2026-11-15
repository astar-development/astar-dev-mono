using System.Windows.Input;
using AStarDev.CloudSync.ViewModels.Accounts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.Layouts;

/// <summary>An account card in layout C's accounts strip. Selecting it shows only that account's sync pairs.</summary>
/// <param name="account">The account.</param>
/// <param name="select">Called with this card when the user selects it.</param>
public sealed class LayoutCAccountCardViewModel(AccountViewModel account, Action<LayoutCAccountCardViewModel> select) : ObservableObject
{
    private bool isSelected;

    /// <summary>The account.</summary>
    public AccountViewModel Account => account;

    /// <summary>Whether this is the selected account.</summary>
    public bool IsSelected
    {
        get => isSelected;
        internal set => SetProperty(ref isSelected, value);
    }

    /// <summary>Selects this account, or clears the selection when it is already selected.</summary>
    public ICommand SelectCommand => field ??= new RelayCommand(() => select(this));
}
