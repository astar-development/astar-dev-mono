using System.IO.Abstractions;
using System.Text.Json;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStarDev.OneDriveSyncClient.Data.Repositories;

namespace AStarDev.OneDriveSyncClient.Classifications;

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
        string json = await fileSystem.File.ReadAllTextAsync(fileInfo.FullName, cancellationToken).ConfigureAwait(false);
        var importRoot = DeserializeImportRoot(json);
        if (importRoot is null)
            return;

        var importedCategories = importRoot.Categories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();
        var existingCategories = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);
        var existingIds = existingCategories.Select(c => c.Id.Value).ToHashSet();
        var importIds = importedCategories.Select(c => c.Id).ToHashSet();

        foreach (var node in importedCategories)
            await InsertNodeAsync(node, Option.None<FileClassificationCategoryId>(), 1, existingIds, cancellationToken).ConfigureAwait(false);

        var fileClassificationCategoryIds = existingCategories
            .Select(c => c.Id)
            .Where(id => !importIds.Contains(id.Value))
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

        int level = node.Level > 0 ? node.Level : UpdateParentLevel(parentLevel, effectiveParentId);
        var fileClassificationCategoryId = FileClassificationCategoryIdFactory.Create(node.Id);

        var createResult = FileClassificationCategoryFactory.Create(fileClassificationCategoryId, node.Name, level, node.IsFamous ?? false, node.IsInternet ?? false, effectiveParentId, node.IncludeInSearch);
        if (createResult is not Ok<FileClassificationCategory, string> { Value: var category })
            return;

        var addResult = await repository.AddCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        if (addResult is not Ok<FileClassificationCategoryId, string> { Value: var newFileClassificationCategoryId })
            return;

        existingIds.Add(newFileClassificationCategoryId.Value);
        foreach (var child in node.Children)
            await InsertNodeAsync(child, Option.Some(newFileClassificationCategoryId), level, existingIds, cancellationToken).ConfigureAwait(false);
    }

    private static int UpdateParentLevel(int parentLevel, Option<FileClassificationCategoryId>? effectiveParentId) => effectiveParentId is Option<FileClassificationCategoryId>.Some ? parentLevel + 1 : 1;

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
            .ToDictionary(category => category.Id.Value, category => new ClassificationCategoryNode
            {
                Id = category.Id.Value,
                Level = category.Level,
                ParentId = category.ParentId.MapOrDefault(id => (int?)id.Value, null),
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
