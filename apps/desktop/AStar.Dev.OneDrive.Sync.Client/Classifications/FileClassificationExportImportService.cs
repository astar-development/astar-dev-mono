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
    public async Task ExportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        var allCategories = await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false);

        var parentIds = allCategories
            .Where(c => c.ParentId is Option<FileClassificationCategoryId>.Some)
            .Select(c => ((Option<FileClassificationCategoryId>.Some)c.ParentId).Value)
            .ToHashSet();

        var nodesByCategoryId = new Dictionary<FileClassificationCategoryId, ClassificationCategoryNode>();
        foreach (var category in allCategories)
            nodesByCategoryId[category.Id] = new ClassificationCategoryNode { Name = category.Name, IsFamous = category.IsFamous, IsInternet = category.IsInternet, Children = [], Keywords = [] };

        foreach (var category in allCategories)
        {
            bool isLeaf = !parentIds.Contains(category.Id);
            if (!isLeaf)
                continue;

            var keywords = await repository.GetKeywordsForCategoryAsync(category.Id, cancellationToken).ConfigureAwait(false);
            nodesByCategoryId[category.Id].Keywords.AddRange(keywords.Select(k => new ClassificationKeywordNode(k.Keyword.Value, k.Keyword.IsFamous is Option<bool>.Some f ? f.Value : null, k.Keyword.IsInternet is Option<bool>.Some i ? i.Value : null)));
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
        fileSystem.File.WriteAllText(fileInfo.FullName, json);
    }

    /// <inheritdoc />
    public async Task ImportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        string json = fileSystem.File.ReadAllText(fileInfo.FullName);
        var exportRoot = JsonSerializer.Deserialize<ClassificationExportRoot>(json, SerializerOptions);
        if (exportRoot is null)
            return;

        await repository.DeleteAllAsync(cancellationToken).ConfigureAwait(false);

        foreach (var node in exportRoot.Categories)
            await InsertNodeAsync(node, 1, false, false, Option.None<FileClassificationCategoryId>(), cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertNodeAsync(ClassificationCategoryNode node, int level, bool isFamous, bool isInternet, Option<FileClassificationCategoryId> parentId, CancellationToken cancellationToken)
    {
        var placeholder = new FileClassificationCategoryId(0);

        await FileClassificationCategoryFactory.Create(placeholder, node.Name, level, isFamous, isInternet, parentId)
            .BindAsync(category => repository.AddCategoryAsync(category, cancellationToken))
            .MatchAsync(
                async newId =>
                {
                    foreach (var child in node.Children)
                        await InsertNodeAsync(child, level + 1, isFamous, isInternet, Option.Some(newId), cancellationToken).ConfigureAwait(false);

                    foreach (var keyword in node.Keywords)
                        await repository.AddKeywordAsync(newId, new FileClassificationKeyword(keyword.Value, keyword.IsFamous.HasValue ? Option.Some(keyword.IsFamous.Value) : Option.None<bool>(), keyword.IsInternet.HasValue ? Option.Some(keyword.IsInternet.Value) : Option.None<bool>()), cancellationToken).ConfigureAwait(false);
                },
                _ => Task.CompletedTask)
            .ConfigureAwait(false);
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
    public bool? IsFamous { get; init; }
    public bool? IsInternet { get; init; }
    public List<ClassificationCategoryNode> Children { get; set; } = [];
    public List<ClassificationKeywordNode> Keywords { get; set; } = [];
}

internal sealed record ClassificationKeywordNode(string Value, bool? IsFamous, bool? IsInternet);
