---
description: Format code according to specified guidelines.
---

# Format Agent

## Mission: Format code according to specified guidelines.

## Guidelines
- apply consistent formatting based on the solution's .editorconfig settings
- a blank line should be added after control statements (e.g., if, else, for, while) / before the next statement
- return statements should always have a blank line before them
- ensure code follows the style conventions below

## Incorrect Record (or class) Formatting
```csharp
public sealed record FolderNodeState(
    string Id,
    string? ParentId,
    string Name,
    bool IsSelected,
    bool IsExpanded,
    int SortOrder);
````

## Correct Record (or class) Formatting
```csharp
public sealed record FolderNodeState(string Id, string? ParentId, string Name, bool IsSelected, bool IsExpanded, int SortOrder);
```

## Incorrect Method Formatting
```csharp
public static bool ReplaceNodeInCollection(
  ObservableCollection<FolderNode> collection,
  FolderNode target,
  FolderNode replacement)
{
  // Implementation omitted for brevity
}
```

## Correct Method Formatting
```csharp
public static bool ReplaceNodeInCollection(ObservableCollection<FolderNode> collection, FolderNode target, FolderNode replacement)
{
  // Implementation omitted for brevity
}
```
