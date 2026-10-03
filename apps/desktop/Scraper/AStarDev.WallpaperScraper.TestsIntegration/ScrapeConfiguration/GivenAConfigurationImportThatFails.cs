using AStarDev.WallpaperScraper.Scoping;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using AStarDev.WallpaperScraper.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AStarDev.WallpaperScraper.TestsIntegration.ScrapeConfiguration;

/// <summary>Importing replaces the stored configuration; when the replacement cannot be saved, the original must still be there.</summary>
public sealed class GivenAConfigurationImportThatFails : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-WallpaperScraper-failed-import-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private bool disposed;

    public GivenAConfigurationImportThatFails()
        => serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();

    [Fact]
    public async Task when_saving_the_replacement_fails_then_the_original_configuration_is_still_stored()
    {
        var originalId = await CreateOriginalAsync();
        var importer = new ScrapeConfigurationImporter(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), TimeProvider.System);

        var result = await importer.ImportScrapeConfigurationAsync(DocumentWithDuplicateCategoryIds(), TestContext.Current.CancellationToken);

        var stored = await StoredConfigurationIdsAsync();
        (result.Match(_ => true, _ => false), stored.Count, stored.Single()).ShouldBe((false, 1, originalId));
    }

    [Fact]
    public async Task when_the_replacement_saves_then_it_replaces_the_original()
    {
        var originalId = await CreateOriginalAsync();
        var importer = new ScrapeConfigurationImporter(new ScopedRunner(serviceProvider.GetRequiredService<IServiceScopeFactory>()), TimeProvider.System);
        var replacementId = Guid.CreateVersion7();

        var result = await importer.ImportScrapeConfigurationAsync(new ScrapeConfigurationImportDocument { Id = replacementId, SearchConfiguration = new SearchConfigurationImportDocument { Id = Guid.CreateVersion7() }, UserConfiguration = new UserConfigurationImportDocument { Id = Guid.CreateVersion7() }, ScrapeDirectories = new ScrapeDirectoriesImportDocument { Id = Guid.CreateVersion7() } }, TestContext.Current.CancellationToken);

        (result.Match(_ => true, _ => false), (await StoredConfigurationIdsAsync()).Single().Value == replacementId, originalId.Value != replacementId).ShouldBe((true, true, true));
    }

    private static ScrapeConfigurationImportDocument DocumentWithDuplicateCategoryIds()
        => new()
        {
            Id = Guid.CreateVersion7(),
            UserConfiguration = new UserConfigurationImportDocument { Id = Guid.CreateVersion7() },
            ScrapeDirectories = new ScrapeDirectoriesImportDocument { Id = Guid.CreateVersion7() },
            SearchConfiguration = new SearchConfigurationImportDocument
            {
                Id = Guid.CreateVersion7(),
                SearchCategories = [new SearchCategoryImportDocument { Id = "same", Name = "one" }, new SearchCategoryImportDocument { Id = "same", Name = "two" }]
            }
        };

    private async Task<ScrapeConfigurationId> CreateOriginalAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        return (await StoredConfigurationIdsAsync()).Single();
    }

    private async Task<IReadOnlyList<ScrapeConfigurationId>> StoredConfigurationIdsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();

        return await context.ScrapeConfigurations.AsNoTracking().Select(configuration => configuration.Id).ToListAsync(TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }
}
