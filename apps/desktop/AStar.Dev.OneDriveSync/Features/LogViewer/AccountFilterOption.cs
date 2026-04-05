using System;

namespace AStar.Dev.OneDriveSync.Features.LogViewer;

/// <summary>A single entry in the account filter dropdown on the Log Viewer (S014).</summary>
public record AccountFilterOption(string AccountId, string DisplayName);

/// <summary>Factory for <see cref="AccountFilterOption"/>.</summary>
public static class AccountFilterOptionFactory
{
    /// <summary>Creates an <see cref="AccountFilterOption"/> for a real account.</summary>
    public static AccountFilterOption Create(string accountId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new AccountFilterOption(accountId, displayName);
    }

    /// <summary>Creates the sentinel "All Accounts" option.</summary>
    public static AccountFilterOption CreateAllAccounts(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new AccountFilterOption(LogViewerViewModel.AllAccounts, displayName);
    }
}
