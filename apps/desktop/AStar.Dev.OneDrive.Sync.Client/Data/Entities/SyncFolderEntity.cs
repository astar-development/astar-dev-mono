using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncFolderEntity
{
    public int Id { get; set; }
    public string FolderId { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? DeltaLink { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
