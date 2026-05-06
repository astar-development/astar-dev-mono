using System.ComponentModel.DataAnnotations.Schema;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>
/// Represents a synchronization rule for a specific OneDrive account, defining the conditions under which certain files or folders should be included or excluded from synchronization. This entity captures details about the account, the remote path to which the rule applies, the type of rule (e.g., include or exclude), and optionally the remote item ID if the rule is specific to a particular item. The SyncRuleEntity allows the sync client to manage and apply synchronization rules effectively, ensuring that only the desired files and folders are synchronized between the local system and OneDrive.
/// </summary>
public sealed class SyncRuleEntity
{
    /// <summary>
    /// The unique identifier for the synchronization rule. This property serves as the primary key for the SyncRuleEntity, allowing for efficient retrieval and management of synchronization rules within the database. The Id is typically generated automatically by the database when a new rule is created, ensuring that each rule has a distinct identifier for reference in operations such as updates, deletions, and associations with other entities like AccountEntity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The identifier of the OneDrive account associated with this synchronization rule. This is a foreign key that links to the AccountEntity, allowing for the association of the synchronization rule with the corresponding account profile and synchronization configuration. It is essential for tracking which rules belong to which accounts, especially in scenarios where multiple OneDrive accounts are being synchronized within the same client application. By storing the AccountId, the sync client can efficiently apply the appropriate rules during synchronization operations based on the account context.
    /// </summary>
    public AccountId AccountId { get; set; }

    /// <summary>
    /// The remote path in OneDrive to which this synchronization rule applies. This property defines the specific location within the OneDrive folder structure that the rule targets, allowing for precise control over which files and folders are included or excluded from synchronization. The RemotePath can be used to specify rules for entire folders (e.g., "/Documents/Work") or for specific items (e.g., "/Documents/Work/Report.docx"), depending on the desired level of granularity for the synchronization rules. By defining the RemotePath, users can ensure that their synchronization preferences are accurately applied during sync operations, helping to manage storage usage and maintain organization within their OneDrive accounts.
    /// </summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>
    /// The type of synchronization rule, indicating whether it is an inclusion or exclusion rule. This property determines how the sync client processes files and folders that match the defined RemotePath during synchronization operations. An inclusion rule (RuleType.Include) means that matching items should be included in the synchronization process, while an exclusion rule (RuleType.Exclude) means that matching items should be excluded from synchronization. By specifying the RuleType, users can customize their synchronization preferences to ensure that only the desired files and folders are synchronized between their local system and OneDrive, providing greater control over their data management and storage usage.
    /// </summary>
    public RuleType RuleType { get; set; }

    /// <summary>
    /// The optional identifier of the specific item in OneDrive to which this synchronization rule applies. This property is used when the synchronization rule is intended to target a specific item rather than a broader path. By providing the RemoteItemId, users can create rules that apply only to a particular file or folder, allowing for more granular control over synchronization behavior. If the RemoteItemId is null, the rule is applied based on the RemotePath alone, affecting all items that match the specified path according to the defined RuleType.
    /// </summary>
    public string? RemoteItemId { get; set; }

    /// <summary>
    /// Navigation property to the associated AccountEntity, allowing for access to the account's profile information, sync configuration, and other related data. This relationship is established through the AccountId foreign key, enabling the sync client to easily retrieve and manage the synchronization rule in the context of the corresponding account. The navigation property is marked as nullable to indicate that there may be cases where the account information is not available or has been deleted, allowing for graceful handling of such scenarios within the application.
    /// </summary>
    [ForeignKey(nameof(AccountId))]
    public AccountEntity? Account { get; set; }
}
