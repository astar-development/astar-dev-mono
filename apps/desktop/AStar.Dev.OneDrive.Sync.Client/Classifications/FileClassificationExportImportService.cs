using System.IO.Abstractions;
using System.Text.Json;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.Infrastructure.AppDb.Domain;
using Microsoft.Extensions.Logging;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Logging;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <inheritdoc />
public sealed class FileClassificationExportImportService(IFileClassificationRepository repository, IFileSystem fileSystem) : IFileClassificationExportImportService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    /// <inheritdoc />
    public async Task ExportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        var allCategories = await repository.GetAllCategoriesSimpleAsync(cancellationToken).ConfigureAwait(false);

        await fileSystem.File.WriteAllTextAsync(fileInfo.FullName, allCategories.ToJsonWithoutNulls(), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ImportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default)
    {
        string json = fileSystem.File.ReadAllText(fileInfo.FullName);
        var importRoot = JsonSerializer.Deserialize<ClassificationExportRoot>(json, SerializerOptions);
        if (importRoot is null)
            return;

        var importedCategories = importRoot.Categories.OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();

        foreach (var node in importedCategories)
            await InsertNodeAsync(node, Option.None<FileClassificationCategoryId>(), cancellationToken).ConfigureAwait(false);

        var existingIds = (await repository.GetAllCategoriesAsync(cancellationToken).ConfigureAwait(false)).Select(c => c.Id.Id).ToHashSet();
        var importIds = importedCategories.Select(c => c.Id).ToHashSet();

        var fileClassificationCategoryIds = existingIds
            .Except(importIds)
            .Select(id => new FileClassificationCategoryId(id))
            .ToList();
        await repository.DeleteAsync(fileClassificationCategoryIds, cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertNodeAsync(ClassificationCategoryNode node, Option<FileClassificationCategoryId> parentId, CancellationToken cancellationToken)
    {
        var fileClassificationCategoryId = FileClassificationCategoryIdFactory.Create(node.Id);

        await FileClassificationCategoryFactory.Create(fileClassificationCategoryId, node.Name, node.Level, node.IsFamous ?? false, node.IsInternet ?? false, parentId, node.IncludeInSearch)
            .BindAsync(category => repository.AddCategoryAsync(category, cancellationToken))
            .MatchAsync(
                async newFileClassificationCategoryId =>
                {
                    foreach (var child in node.Children)
                        await InsertNodeAsync(child, Option.Some(newFileClassificationCategoryId), cancellationToken).ConfigureAwait(false);

                    // foreach (var keyword in node.Keywords) - what did this do? Is it, as I think, legacy from when we used multiple tables?
                    //     await repository.AddKeywordAsync(newId, new FileClassificationKeyword(keyword.Value, keyword.IsFamous.HasValue ? Option.Some(keyword.IsFamous.Value) : Option.None<bool>(), keyword.IsInternet.HasValue ? Option.Some(keyword.IsInternet.Value) : Option.None<bool>()), cancellationToken).ConfigureAwait(false);
                },
                _ => Task.CompletedTask)
            .ConfigureAwait(false);
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
