using System.Diagnostics.CodeAnalysis;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>A connected cloud storage account.</summary>
public sealed record CloudAccount
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Internal constructor called only by CloudAccountFactory; records cannot have an internal primary constructor (#1453).")]
    internal CloudAccount(
        AccountId id,
        string displayName,
        string email,
        CloudProvider provider,
        StorageUsage storage,
        int folderCount,
        int fileCount,
        SyncStatus status,
        Option<DateTimeOffset> lastSync,
        AccountSignInState signIn)
    {
        Id = id;
        DisplayName = displayName;
        Email = email;
        Provider = provider;
        Storage = storage;
        FolderCount = folderCount;
        FileCount = fileCount;
        Status = status;
        LastSync = lastSync;
        SignIn = signIn;
    }

    /// <summary>Whether the account can get tokens without the user (#1393); composed in by the overview.</summary>
    public AccountSignInState SignIn { get; }

    /// <summary>The account id.</summary>
    public AccountId Id { get; }

    /// <summary>The account's display name.</summary>
    public string DisplayName { get; }

    /// <summary>The account's sign-in email.</summary>
    public string Email { get; }

    /// <summary>The provider the account belongs to.</summary>
    public CloudProvider Provider { get; }

    /// <summary>Cloud storage used and available.</summary>
    public StorageUsage Storage { get; }

    /// <summary>Folders being synced.</summary>
    public int FolderCount { get; }

    /// <summary>Files being synced.</summary>
    public int FileCount { get; }

    /// <summary>The account's sync status.</summary>
    public SyncStatus Status { get; }

    /// <summary>When the account last finished a sync, if ever.</summary>
    public Option<DateTimeOffset> LastSync { get; }
}
