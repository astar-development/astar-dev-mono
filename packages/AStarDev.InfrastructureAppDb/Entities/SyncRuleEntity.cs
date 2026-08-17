using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.FunctionalParadigm;

namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents a synchronization rule for a specific OneDrive account.
/// </summary>
public sealed class SyncRuleEntity
{
    /// <summary>Unique identifier for the synchronization rule.</summary>
    public int Id { get; set; }

    /// <summary>The identifier of the OneDrive account associated with this rule.</summary>
    public AccountId AccountId { get; set; }

    /// <summary>The remote path in OneDrive to which this rule applies.</summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>The type of synchronization rule (include or exclude).</summary>
    public RuleType RuleType { get; set; }

    /// <summary>The specific remote item ID this rule targets, or None if path-based only.</summary>
    public Option<string> RemoteItemId { get; set; } = Option.None<string>();

    /// <summary>Navigation property to the associated AccountEntity.</summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
