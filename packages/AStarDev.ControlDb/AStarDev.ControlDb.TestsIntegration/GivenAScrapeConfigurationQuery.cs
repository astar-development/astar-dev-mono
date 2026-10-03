using System.Data.Common;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb.TestsIntegration.TestDataFactories;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AStarDev.ControlDb.TestsIntegration;

public sealed class GivenAScrapeConfigurationQuery : IDisposable
{
    private static readonly Guid LowestId = Guid.Parse("11111111-1111-7111-8111-111111111111");
    private static readonly Guid HighestId = Guid.Parse("eeeeeeee-eeee-7eee-8eee-eeeeeeeeeeee");

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-controldb-configuration-query-{Guid.CreateVersion7():N}.db");
    private readonly CommandTextRecorder recorder = new();
    private readonly ControlDbContext context;
    private bool disposed;

    public GivenAScrapeConfigurationQuery()
    {
        using var setup = NewContext();
        _ = setup.Database.EnsureDeleted();
        _ = setup.Database.EnsureCreated();
        _ = setup.ScrapeConfigurations.ExecuteDelete(); // EnsureCreated seeds a default configuration; these tests control the rows themselves.
        context = NewContext();
    }

    [Fact]
    public async Task when_a_configuration_has_several_categories_of_both_kinds_then_all_of_them_are_loaded()
    {
        await AddConfigurationWithCategoriesAsync(LowestId, 3, 4);

        var configuration = await LoadFirstAsync();

        configuration.SearchConfiguration.SearchCategories.Select(category => category.Name).Order().ShouldBe(["search-0", "search-1", "search-2"]);
        configuration.SearchConfiguration.PersonCategories.Select(category => category.Name).Order().ShouldBe(["person-0", "person-1", "person-2", "person-3"]);
    }

    [Fact]
    public async Task when_a_configuration_is_loaded_then_the_two_category_collections_are_not_read_by_the_same_command()
    {
        await AddConfigurationWithCategoriesAsync(LowestId, 3, 4);
        recorder.Clear();

        _ = await LoadFirstAsync();

        var searchTable = context.Model.FindEntityType(typeof(SearchCategoryEntity))!.GetTableName()!;
        var personTable = context.Model.FindEntityType(typeof(PersonCategoryEntity))!.GetTableName()!;
        recorder.Commands.ShouldContain(command => command.Contains(searchTable, StringComparison.Ordinal));
        recorder.Commands.ShouldContain(command => command.Contains(personTable, StringComparison.Ordinal));
        recorder.Commands.ShouldNotContain(command => command.Contains(searchTable, StringComparison.Ordinal) && command.Contains(personTable, StringComparison.Ordinal));
    }

    [Fact]
    public async Task when_several_configurations_exist_then_the_first_loaded_is_the_one_with_the_lowest_id()
    {
        await AddConfigurationWithCategoriesAsync(HighestId, 1, 1);
        await AddConfigurationWithCategoriesAsync(LowestId, 1, 1);

        var configuration = await LoadFirstAsync();

        configuration.Id.Value.ShouldBe(LowestId);
    }

    private async Task<ScrapeConfigurationEntity> LoadFirstAsync()
    {
        using var freshContext = NewContext();
        var repository = freshContext.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>();

        var result = await repository.TryGetFirstAsync(TestContext.Current.CancellationToken);

        return result.Match(option => option, exception => throw exception).Match(entity => entity, () => throw new InvalidOperationException("No scrape configuration was loaded."));
    }

    private async Task AddConfigurationWithCategoriesAsync(Guid id, int searchCategories, int personCategories)
    {
        var entity = ScrapeConfigurationEntityFactory.CreateScrapeConfigurationEntity(id);
        for (var i = 0; i < searchCategories; i++) entity.SearchConfiguration.SearchCategories.Add(new SearchCategoryEntity { Id = $"{id:N}-{i}", Name = $"search-{i}" });
        for (var i = 0; i < personCategories; i++) entity.SearchConfiguration.PersonCategories.Add(new PersonCategoryEntity { Name = $"person-{i}" });

        _ = await context.ScrapeConfigurations.AddAsync(entity, TestContext.Current.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private ControlDbContext NewContext() => new(new DbContextOptionsBuilder<ControlDbContext>()
        .UseSqlite($"Data Source={databasePath}")
        .AddInterceptors(recorder)
        .Options);

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        context.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private sealed class CommandTextRecorder : DbCommandInterceptor
    {
        private readonly List<string> commands = [];

        public IReadOnlyList<string> Commands => commands;

        public void Clear() => commands.Clear();

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            commands.Add(command.CommandText);

            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            commands.Add(command.CommandText);

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
