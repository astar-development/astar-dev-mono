using AStarDev.CloudSync.Accounts;
using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.DisplaySettings;

/// <summary>One account the audit can be run for (#1801).</summary>
public sealed class AuditAccountViewModel
{
    internal AuditAccountViewModel(AccountId id, string name, bool isSelected, Action select)
    {
        Id = id;
        Name = name;
        IsSelected = isSelected;
        SelectCommand = new RelayCommand(select);
    }

    /// <summary>The account.</summary>
    public AccountId Id { get; }

    /// <summary>The account's display name.</summary>
    public string Name { get; }

    /// <summary>Whether the audit will run for this account.</summary>
    public bool IsSelected { get; }

    /// <summary>Chooses this account.</summary>
    public IRelayCommand SelectCommand { get; }
}
