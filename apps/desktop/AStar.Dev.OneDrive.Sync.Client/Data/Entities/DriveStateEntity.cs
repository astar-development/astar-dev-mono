using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class DriveStateEntity
{
    public int Id { get; set; }
    public AccountId AccountId { get; set; }
    public string? DeltaLink { get; set; }
    public DateTimeOffset? LastSyncStartedAt { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
