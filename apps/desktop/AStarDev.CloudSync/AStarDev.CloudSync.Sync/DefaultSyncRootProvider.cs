using System.Text;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
/// <param name="homeFolder">The folder the default root lives under.</param>
/// <param name="policy">Rejects a candidate that crosses an existing root.</param>
public sealed class DefaultSyncRootProvider(IHomeFolder homeFolder, ISyncRootPolicy policy) : IDefaultSyncRootProvider
{
    private const string RootFolderName = "CloudSync";
    private const string FallbackName = "Account";
    private const int MaxAttempts = 1000;
    private static readonly char[] UnsafeCharacters = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];
    private static readonly AccountId Unassigned = AccountIdFactory.Create(Guid.Empty);

    /// <inheritdoc />
    public Result<SyncRoot, SyncRootRejection> CreateFor(AccountRegistration registration, IReadOnlyList<AccountSyncRoot> existing) =>
        homeFolder.Folder.Match(home => FirstFree(home, FolderName(registration), existing ?? []), () => Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable()));

    private static string FolderName(AccountRegistration registration) => $"{ProviderName(registration.Provider)}-{SafeName(registration.DisplayName)}";

    private static string ProviderName(CloudProvider provider) => provider switch
    {
        CloudProvider.OneDrive => "OneDrive",
        _ => "Cloud",
    };

    private static string SafeName(string? displayName)
    {
        var replaced = new string([.. (displayName ?? string.Empty).Normalize(NormalizationForm.FormC).Select(character => char.IsControl(character) || UnsafeCharacters.Contains(character) ? '_' : character)]);
        var trimmed = replaced.Trim().TrimEnd('.', ' ');

        return trimmed.Length == 0 ? FallbackName : trimmed;
    }

    private Result<SyncRoot, SyncRootRejection> FirstFree(IDirectoryInfo home, string name, IReadOnlyList<AccountSyncRoot> existing) =>
        Enumerable.Range(1, MaxAttempts)
            .Select(attempt => policy.Check(Unassigned, home.FileSystem.DirectoryInfo.New(home.FileSystem.Path.Join(home.FullName, RootFolderName, attempt == 1 ? name : $"{name}-{attempt}")), existing))
            .SelectMany(checkedRoot => checkedRoot.Match(root => new[] { root }, _ => []))
            .FirstOrNone()
            .Match(Result.Success<SyncRoot, SyncRootRejection>, () => Result.Failure<SyncRoot, SyncRootRejection>(SyncRootRejectionFactory.CreateUnavailable()));
}
