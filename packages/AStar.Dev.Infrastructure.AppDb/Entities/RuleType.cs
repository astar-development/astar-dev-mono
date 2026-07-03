namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>
/// Represents the type of a synchronization rule, indicating whether it is an inclusion or exclusion rule. This is used to determine how files and folders are processed during synchronization based on the defined rules in the sync configuration.
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Indicates that the rule is an inclusion rule, meaning that files or folders matching this rule should be included in the synchronization process. This allows users to specify certain items that they want to ensure are always synchronized between their local storage and OneDrive.
    /// </summary>
    Include,

    /// <summary>
    /// Indicates that the rule is an exclusion rule, meaning that files or folders matching this rule should be excluded from the synchronization process. This allows users to specify certain items that they do not want to be synchronized, effectively ignoring them during the sync operation.
    /// </summary>
    Exclude
}
