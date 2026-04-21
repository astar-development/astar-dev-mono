using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncRuleEntity
{
    public int Id { get; set; }
    public AccountId AccountId { get; set; }
    public string RemotePath { get; set; } = string.Empty;
    public RuleType RuleType { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
