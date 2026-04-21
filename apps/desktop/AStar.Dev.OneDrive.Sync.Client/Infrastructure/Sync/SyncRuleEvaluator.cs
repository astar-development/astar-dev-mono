using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;

/// <summary>
/// Stateless evaluator for sync include/exclude rules.
/// Most-specific (longest) matching path prefix wins. Tie-break: Exclude wins.
/// No match means Exclude (default-deny).
/// </summary>
public static class SyncRuleEvaluator
{
    /// <summary>Returns true if the given remote path should be synced according to the supplied rules.</summary>
    public static bool IsIncluded(string remotePath, IReadOnlyList<SyncRuleEntity> rules)
    {
        SyncRuleEntity? match = null;

        foreach(var rule in rules)
        {
            if(!remotePath.StartsWith(rule.RemotePath, StringComparison.OrdinalIgnoreCase))
                continue;

            var afterPrefix = remotePath.Length > rule.RemotePath.Length
                ? remotePath[rule.RemotePath.Length]
                : (char)0;
            if(remotePath.Length > rule.RemotePath.Length && afterPrefix != '/')
                continue;

            if(match is null || rule.RemotePath.Length > match.RemotePath.Length)
                match = rule;
            else if(rule.RemotePath.Length == match.RemotePath.Length && rule.RuleType == RuleType.Exclude)
                match = rule;
        }

        return match?.RuleType == RuleType.Include;
    }
}
