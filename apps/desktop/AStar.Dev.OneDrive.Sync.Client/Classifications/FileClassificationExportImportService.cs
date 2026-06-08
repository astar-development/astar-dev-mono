using System.IO.Abstractions;
using System.Text.Json;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <inheritdoc />
public sealed class FileClassificationExportImportService(IFileClassificationRepository repository, IFileSystem fileSystem) : IFileClassificationExportImportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    /// <inheritdoc />
    public async Task ExportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var allCategories = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);

        var parentIds = allCategories
            .Where(c => c.ParentId is Option<FileClassificationCategoryId>.Some)
            .Select(c => ((Option<FileClassificationCategoryId>.Some)c.ParentId).Value)
            .ToHashSet();

        var nodesByCategoryId = new Dictionary<FileClassificationCategoryId, ClassificationCategoryNode>();
        foreach (var category in allCategories)
            nodesByCategoryId[category.Id] = new ClassificationCategoryNode { Name = category.Name, Children = [], Keywords = [] };

        foreach (var category in allCategories)
        {
            bool isLeaf = !parentIds.Contains(category.Id);
            if (!isLeaf)
                continue;

            var keywords = await repository.GetKeywordsForCategoryAsync(category.Id, cancellationToken).ConfigureAwait(false);
            nodesByCategoryId[category.Id].Keywords.AddRange(keywords.Select(k => new ClassificationKeywordNode(k.Keyword.Value, k.Keyword.IsSpecialOverride is Option<bool>.Some s ? s.Value : null)));
        }

        var rootNodes = new List<ClassificationCategoryNode>();
        foreach (var category in allCategories)
        {
            var node = nodesByCategoryId[category.Id];
            if (category.ParentId is Option<FileClassificationCategoryId>.Some someParent && nodesByCategoryId.TryGetValue(someParent.Value, out var parentNode))
                parentNode.Children.Add(node);
            else
                rootNodes.Add(node);
        }

        var exportRoot = new ClassificationExportRoot { Version = 1, Categories = rootNodes };
        string json = JsonSerializer.Serialize(exportRoot, SerializerOptions);
        fileSystem.File.WriteAllText(filePath, json);
    }

    /// <inheritdoc />
    public async Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string json = fileSystem.File.ReadAllText(filePath);
        var exportRoot = JsonSerializer.Deserialize<ClassificationExportRoot>(json, SerializerOptions);
        if (exportRoot is null)
            return;

        await repository.DeleteAllAsync(cancellationToken).ConfigureAwait(false);

        foreach (var node in exportRoot.Categories)
            await InsertNodeAsync(node, 1, Option.None<FileClassificationCategoryId>(), cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertNodeAsync(ClassificationCategoryNode node, int level, Option<FileClassificationCategoryId> parentId, CancellationToken cancellationToken)
    {
        var placeholder = new FileClassificationCategoryId(0);
        var categoryResult = FileClassificationCategoryFactory.Create(placeholder, node.Name, level, parentId);
        if (categoryResult is not Result<FileClassificationCategory, string>.Ok okCategory)
            return;

        var addResult = await repository.AddCategoryAsync(okCategory.Value, cancellationToken).ConfigureAwait(false);
        if (addResult is not Result<FileClassificationCategoryId, string>.Ok okId)
            return;

        FileClassificationCategoryId newId = okId.Value;

        foreach (var child in node.Children)
            await InsertNodeAsync(child, level + 1, Option.Some(newId), cancellationToken).ConfigureAwait(false);

        foreach (var keyword in node.Keywords)
            await repository.AddKeywordAsync(newId, new FileClassificationKeyword(keyword.Value, keyword.IsSpecialOverride.HasValue ? Option.Some(keyword.IsSpecialOverride.Value) : Option.None<bool>()), cancellationToken).ConfigureAwait(false);
    }
}

internal sealed record ClassificationExportRoot
{
    public int Version { get; init; }
    public List<ClassificationCategoryNode> Categories { get; init; } = [];
}

internal sealed record ClassificationCategoryNode
{
    public string Name { get; init; } = string.Empty;
    public List<ClassificationCategoryNode> Children { get; set; } = [];
    public List<ClassificationKeywordNode> Keywords { get; set; } = [];
}

internal sealed record ClassificationKeywordNode(string Value, bool? IsSpecialOverride);
