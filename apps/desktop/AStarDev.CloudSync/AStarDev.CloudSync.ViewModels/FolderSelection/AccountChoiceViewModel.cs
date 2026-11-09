using CommunityToolkit.Mvvm.Input;

namespace AStarDev.CloudSync.ViewModels.FolderSelection;

/// <summary>One account the user can pick folders for.</summary>
public sealed class AccountChoiceViewModel
{
    internal AccountChoiceViewModel(string name, string email, IAsyncRelayCommand chooseCommand)
    {
        Name = name;
        Email = email;
        ChooseCommand = chooseCommand;
    }

    /// <summary>The account's display name.</summary>
    public string Name { get; }

    /// <summary>The account's sign-in email.</summary>
    public string Email { get; }

    /// <summary>Opens the account's folder tree.</summary>
    public IAsyncRelayCommand ChooseCommand { get; }
}
