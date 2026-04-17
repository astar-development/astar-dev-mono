using System.ComponentModel.DataAnnotations.Schema;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

public sealed class SyncFolderEntity
{
    public int Id { get; set; }
    public OneDriveFolderId FolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;
    public AccountId AccountId { get; set; }
    public string? DeltaLink { get; set; }
    public bool IsExplicitlyExcluded { get; set; }

    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
