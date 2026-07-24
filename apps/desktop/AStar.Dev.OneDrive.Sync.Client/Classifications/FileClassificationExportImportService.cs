using System.IO.Abstractions;
using System.Text.Json;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <inheritdoc />
public sealed class FileClassificationExportImportService(IFileClassificationRepository repository, IFileSystem fileSystem) : IFileClassificationExportImportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    /// <inheritdoc />
    public async Task ExportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        var categories = await BuildExportCategoriesAsync(cancellationToken).ConfigureAwait(false);
        var exportRoot = new ClassificationExportRoot { Version = 1, Categories = categories };
        await fileSystem.File.WriteAllTextAsync(fileInfo.FullName, JsonSerializer.Serialize(exportRoot, SerializerOptions), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ImportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        string json = fileSystem.File.ReadAllText(fileInfo.FullName);
        var importRoot = DeserializeImportRoot(json);
        if (importRoot is null)
            return;

        var importedCategories = importRoot.Categories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();
        var existingCategories = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);
        var existingIds = existingCategories.Select(c => c.Id.Id).ToHashSet();
        var importIds = importedCategories.Select(c => c.Id).ToHashSet();

        foreach (var node in importedCategories)
            await InsertNodeAsync(node, Option.None<FileClassificationCategoryId>(), 1, existingIds, cancellationToken).ConfigureAwait(false);

        var fileClassificationCategoryIds = existingCategories
            .Select(c => c.Id)
            .Where(id => !importIds.Contains(id.Id))
            .ToList();
        await repository.DeleteAsync(fileClassificationCategoryIds, cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertNodeAsync(ClassificationCategoryNode node, Option<FileClassificationCategoryId> parentId, int parentLevel, HashSet<int> existingIds, CancellationToken cancellationToken)
    {
        if (existingIds.Contains(node.Id))
        {
            foreach (var child in node.Children)
                await InsertNodeAsync(child, Option.Some(FileClassificationCategoryIdFactory.Create(node.Id)), parentLevel + 1, existingIds, cancellationToken).ConfigureAwait(false);

            return;
        }

        var effectiveParentId = parentId;
        if (effectiveParentId is Option<FileClassificationCategoryId>.None && node.ParentId.HasValue)
            effectiveParentId = Option.Some(FileClassificationCategoryIdFactory.Create(node.ParentId.Value));

        int level = node.Level > 0 ? node.Level : (effectiveParentId is Option<FileClassificationCategoryId>.Some ? parentLevel + 1 : 1);
        var fileClassificationCategoryId = FileClassificationCategoryIdFactory.Create(node.Id);

        var createResult = FileClassificationCategoryFactory.Create(fileClassificationCategoryId, node.Name, level, node.IsFamous ?? false, node.IsInternet ?? false, effectiveParentId, node.IncludeInSearch);
        if (createResult is not Result<FileClassificationCategory, string>.Ok { Value: var category })
            return;

        var addResult = await repository.AddCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        if (addResult is not Result<FileClassificationCategoryId, string>.Ok { Value: var newFileClassificationCategoryId })
            return;

        existingIds.Add(newFileClassificationCategoryId.Id);
        foreach (var child in node.Children)
            await InsertNodeAsync(child, Option.Some(newFileClassificationCategoryId), level, existingIds, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<ClassificationCategoryNode>> BuildExportCategoriesAsync(CancellationToken cancellationToken)
    {
        var simpleCategories = await repository.GetAllCategoriesSimpleAsync(cancellationToken).ConfigureAwait(false);
        if (simpleCategories is { Count: > 0 })
            return BuildCategoryHierarchy(simpleCategories);

        var categories = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);
        return BuildCategoryHierarchy(categories);
    }

    private static List<ClassificationCategoryNode> BuildCategoryHierarchy(IReadOnlyList<FileClassificationCategoryEntity> categories)
    {
        var nodesById = categories
            .OrderBy(category => category.Level)
            .ThenBy(category => category.Name)
            .ToDictionary(category => category.Id, category => new ClassificationCategoryNode
            {
                Id = category.Id,
                Level = category.Level,
                ParentId = category.ParentId,
                Name = category.Name,
                IsFamous = category.IsFamous,
                IsInternet = category.IsInternet,
                IncludeInSearch = category.IncludeInSearch,
            });

        List<ClassificationCategoryNode> roots = [];
        foreach (var node in nodesById.Values)
        {
            if (node.ParentId.HasValue && nodesById.TryGetValue(node.ParentId.Value, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }

    private static List<ClassificationCategoryNode> BuildCategoryHierarchy(IReadOnlyList<FileClassificationCategory> categories)
    {
        var nodesById = categories
            .OrderBy(category => category.Level)
            .ThenBy(category => category.Name)
            .ToDictionary(category => category.Id.Id, category => new ClassificationCategoryNode
            {
                Id = category.Id.Id,
                Level = category.Level,
                ParentId = category.ParentId.MapOrDefault(id => (int?)id.Id, null),
                Name = category.Name,
                IsFamous = category.IsFamous,
                IsInternet = category.IsInternet,
                IncludeInSearch = category.IncludeInSearch,
            });

        List<ClassificationCategoryNode> roots = [];
        foreach (var node in nodesById.Values)
        {
            if (node.ParentId.HasValue && nodesById.TryGetValue(node.ParentId.Value, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        return roots;
    }

    private static ClassificationExportRoot? DeserializeImportRoot(string json)
    {
        var wrapper = JsonSerializer.Deserialize<ClassificationExportRoot>(json, SerializerOptions);
        if (wrapper is not null)
            return wrapper;

        var legacyCategories = JsonSerializer.Deserialize<List<ClassificationCategoryNode>>(json, SerializerOptions);
        return legacyCategories is null ? null : new ClassificationExportRoot { Version = 1, Categories = legacyCategories };
    }
}

internal class ClassificationCategoryNodeComparer : IEqualityComparer<ClassificationCategoryNode>
{
    public bool Equals(ClassificationCategoryNode? x, ClassificationCategoryNode? y) => x?.Id == y?.Id;

    public int GetHashCode(ClassificationCategoryNode obj) => obj.Id.GetHashCode();
}

internal sealed record ClassificationExportRoot
{
    public int Version { get; init; }
    public List<ClassificationCategoryNode> Categories { get; init; } = [];
}

internal sealed record ClassificationCategoryNode
{
    public int Id { get; init; }
    public int Level { get; init; }
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool? IsFamous { get; init; }
    public bool? IsInternet { get; init; }
    public bool IncludeInSearch { get; init; }
    public List<ClassificationCategoryNode> Children { get; set; } = [];
    public List<ClassificationKeywordNode> Keywords { get; set; } = [];
}

internal sealed record ClassificationKeywordNode(string Value, bool? IsFamous, bool? IsInternet);
