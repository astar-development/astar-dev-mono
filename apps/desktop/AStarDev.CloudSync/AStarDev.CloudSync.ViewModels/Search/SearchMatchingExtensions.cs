using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.CloudSync.ViewModels.SyncPairs;

namespace AStarDev.CloudSync.ViewModels.Search;

/// <summary>The rule every layout's search uses: the text contains the query, ignoring case and surrounding spaces; a blank query matches everything.</summary>
public static class SearchMatchingExtensions
{
    /// <summary>Whether the text contains the query, ignoring case and the query's surrounding spaces.</summary>
    /// <param name="text">The text to test.</param>
    /// <param name="query">What the user typed; blank matches everything.</param>
    public static bool MatchesSearch(this string text, string query) => text.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the account's name, email or provider matches the query.</summary>
    /// <param name="account">The account to test.</param>
    /// <param name="query">What the user typed; blank matches everything.</param>
    public static bool MatchesSearch(this AccountViewModel account, string query) => account.Name.MatchesSearch(query) || account.Email.MatchesSearch(query) || account.Provider.Name.MatchesSearch(query);

    /// <summary>Whether the sync pair's name, local path or remote path matches the query.</summary>
    /// <param name="pair">The sync pair to test.</param>
    /// <param name="query">What the user typed; blank matches everything.</param>
    public static bool MatchesSearch(this SyncPairViewModel pair, string query) => pair.Name.MatchesSearch(query) || pair.LocalPath.MatchesSearch(query) || pair.RemotePath.MatchesSearch(query);
}
