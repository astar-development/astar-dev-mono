using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.Sync;

/// <summary>What a resolution reads once before it transfers: the account, the sync root with the remote index rows of the affected paths and their parents, and the provider capabilities.</summary>
/// <param name="accountId">The account.</param>
/// <param name="input">The root and the remote index rows.</param>
/// <param name="providers">The provider capabilities.</param>
internal sealed class ResolutionContext(AccountId accountId, UploadPassInput input, UploadProviders providers)
{
    internal AccountId AccountId { get; } = accountId;

    internal UploadPassInput Input { get; } = input;

    internal UploadProviders Providers { get; } = providers;
}
