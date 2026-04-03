using System;
using System.Reactive;
using AStar.Dev.OneDrive.Client.Features.Authentication;
using AStar.Dev.OneDriveSync.Infrastructure;
using AStar.Dev.OneDriveSync.Infrastructure.Localisation;
using ReactiveUI;

namespace AStar.Dev.OneDriveSync.Features.Accounts;

/// <summary>
///     View model for a single account row in the Accounts view (Section 7, AM-08).
/// </summary>
public sealed class AccountRowViewModel : ViewModelBase
{
    private readonly Account _account;

    public AccountRowViewModel(Account account, IRelativeTimeFormatter timeFormatter, Action<AccountRowViewModel> onRemove, Action<AccountRowViewModel> onSyncNow)
    {
        _account = account;

        DisplayName    = account.DisplayName;
        MaskedEmail    = MaskEmail(account.Email);
        IsAuthRequired = account.AuthState == nameof(AccountAuthState.AuthRequired);
        CanRemove      = !account.IsSyncActive;
        LastSynced     = account.LastSyncedAt.HasValue
            ? timeFormatter.Format(account.LastSyncedAt.Value, DateTimeOffset.UtcNow)
            : null;

        RemoveCommand  = ReactiveCommand.Create(() => onRemove(this));
        SyncNowCommand = ReactiveCommand.Create(() => onSyncNow(this));
    }

    public Guid AccountId => _account.Id;

    public string DisplayName { get; }

    public string MaskedEmail { get; }

    public bool IsAuthRequired
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool CanRemove
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? LastSynced { get; }

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

    public ReactiveCommand<Unit, Unit> SyncNowCommand { get; }

    internal void UpdateAuthState(AccountAuthState newState)
        => IsAuthRequired = newState == AccountAuthState.AuthRequired;

    private static string MaskEmail(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return email;

        return email[0] + "***" + email[atIndex..];
    }
}
