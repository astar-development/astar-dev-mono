using System.IO.Abstractions;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;

/// <summary>
///     Builds the <see cref="EntityEditorViewModel{TEntity}" /> for each of the tables editable from the
///     main window's Configuration menu.
/// </summary>
/// <param name="dbContextFactory">Passed through to every editor so each opens its own tracked <see cref="AppDbContext" />.</param>
/// <param name="fileSystem">Passed through to every editor for JSON import/export.</param>
/// <param name="exportDirectory">The directory every editor's Import/Export command reads from and writes to.</param>
public sealed class EntityEditorFactory(IDbContextFactory<AppDbContext> dbContextFactory, IFileSystem fileSystem, ExportDirectory exportDirectory) : IEntityEditorFactory
{
    /// <inheritdoc />
    public EntityEditorViewModelBase CreateConnectionStringsEditor() =>
        new EntityEditorViewModel<ConnectionStringsEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<ConnectionStringsEntity>(
                DisplayName: "Connection Strings Configuration",
                TableName: "ConnectionStrings",
                CreateNew: () => new ConnectionStringsEntity(),
                AllowAddRemove: false,
                ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
                ReadOnlyColumns: [nameof(ConnectionStringsEntity.Id)]),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateFileClassificationCategoriesEditor() =>
        new EntityEditorViewModel<FileClassificationCategoryEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<FileClassificationCategoryEntity>(
                DisplayName: "File Classification Categories Configuration",
                TableName: "FileClassificationCategories",
                CreateNew: () => new FileClassificationCategoryEntity(),
                AllowAddRemove: true,
                ExcludedColumns: [nameof(FileClassificationCategoryEntity.Parent)],
                ReadOnlyColumns: [nameof(FileClassificationCategoryEntity.Id)],
                CustomActionLabel: "Sync Tags to Ignore",
                CustomActionAsync: SyncTagsToIgnoreAsync,
                OrderItemsBy: classification => classification.Name),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateSearchConfigurationEditor() =>
        new EntityEditorViewModel<SearchConfigurationEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<SearchConfigurationEntity>(
                DisplayName: "Search Configuration",
                TableName: "SearchConfiguration",
                CreateNew: () => new SearchConfigurationEntity(),
                AllowAddRemove: false,
                ExcludedColumns:
                [
                    nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt), nameof(SearchConfigurationEntity.SearchCategories)
                ],
                ReadOnlyColumns: [nameof(SearchConfigurationEntity.Id)]),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateModelToIgnoreEditor() =>
        new EntityEditorViewModel<ModelToIgnoreEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<ModelToIgnoreEntity>(
                DisplayName: "Model to Ignore",
                TableName: "ModelToIgnore",
                CreateNew: () => new ModelToIgnoreEntity(),
                AllowAddRemove: true,
                ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
                ReadOnlyColumns: [nameof(ModelToIgnoreEntity.Id)]),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateScrapeDirectoriesEditor() =>
        new EntityEditorViewModel<ScrapeDirectoriesEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<ScrapeDirectoriesEntity>(
                DisplayName: "Scrape Directories",
                TableName: "ScrapeDirectories",
                CreateNew: () => new ScrapeDirectoriesEntity(),
                AllowAddRemove: false,
                ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
                ReadOnlyColumns: [nameof(ScrapeDirectoriesEntity.Id)]),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateSearchCategoriesEditor() =>
        new EntityEditorViewModel<SearchCategoryEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<SearchCategoryEntity>(
                DisplayName: "Search Categories",
                TableName: "SearchCategories",
                CreateNew: () => new SearchCategoryEntity { Id = Guid.NewGuid().ToString() },
                AllowAddRemove: true,
                ExcludedColumns:
                [
                    nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt),
                    nameof(SearchCategoryEntity.SearchConfiguration), nameof(SearchCategoryEntity.SearchConfigurationId)
                ],
                ReadOnlyColumns: [nameof(SearchCategoryEntity.Id)],
                OnBeforeAddAsync: async (context, entity, token) =>
                    entity.SearchConfigurationId = await context.Set<SearchConfigurationEntity>().Select(configuration => configuration.Id).FirstAsync(token)),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateTagToIgnoreEditor() =>
        new EntityEditorViewModel<TagToIgnoreEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<TagToIgnoreEntity>(
                DisplayName: "Tag to Ignore",
                TableName: "TagToIgnore",
                CreateNew: () => new TagToIgnoreEntity(),
                AllowAddRemove: true,
                ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
                ReadOnlyColumns: [nameof(TagToIgnoreEntity.Id)]),
            fileSystem,
            exportDirectory.Value);

    /// <inheritdoc />
    public EntityEditorViewModelBase CreateUserConfigurationEditor() =>
        new EntityEditorViewModel<UserConfigurationEntity>(
            dbContextFactory,
            new EntityEditorDescriptor<UserConfigurationEntity>(
                DisplayName: "User Configuration",
                TableName: "UserConfiguration",
                CreateNew: () => new UserConfigurationEntity(),
                AllowAddRemove: false,
                ExcludedColumns: [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.UpdatedAt)],
                ReadOnlyColumns: [nameof(UserConfigurationEntity.Id)]),
            fileSystem,
            exportDirectory.Value);

    private static async Task<string> SyncTagsToIgnoreAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        await context.Set<FileClassificationCategoryEntity>().LoadAsync(cancellationToken);
        await context.Set<TagToIgnoreEntity>().LoadAsync(cancellationToken);

        var classifications = context.Set<FileClassificationCategoryEntity>().Local.Where(classification => !string.IsNullOrWhiteSpace(classification.Name)).ToList();
        var tagsToIgnore = context.Set<TagToIgnoreEntity>().Local.ToList();
        int addedCount = 0;
        int removedCount = 0;

        foreach (var classification in classifications)
        {
            var matchingTags = tagsToIgnore.Where(tag => tag.Value.Equals(classification.Name, StringComparison.OrdinalIgnoreCase)).ToList();

            if (classification.IncludeInSearch)
            {
                removedCount += RemoveMatchingTags(context, tagsToIgnore, matchingTags);
            }
            else if (matchingTags.Count == 0)
            {
                var tag = new TagToIgnoreEntity { Value = classification.Name };
                context.Add(tag);
                tagsToIgnore.Add(tag);
                addedCount++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return $"Synced tags to ignore: {addedCount} added, {removedCount} removed.";
    }

    private static int RemoveMatchingTags(AppDbContext context, List<TagToIgnoreEntity> tagsToIgnore, List<TagToIgnoreEntity> matchingTags)
    {
        foreach (var tag in matchingTags)
        {
            context.Remove(tag);
            tagsToIgnore.Remove(tag);
        }

        return matchingTags.Count;
    }
}
