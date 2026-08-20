using AStar.Dev.File.App.Data;

namespace AStar.Dev.File.App.TestsUnit.Data;

public class GivenADesignTimeDbContextFactory
{
    private readonly DesignTimeDbContextFactory _sut = new();

    [Fact]
    public void when_creating_a_db_context_then_a_file_app_db_context_is_returned()
    {
        using var dbContext = _sut.CreateDbContext([]);

        dbContext.ShouldNotBeNull();
        dbContext.ShouldBeOfType<FileAppDbContext>();
    }

    [Fact]
    public void when_creating_a_db_context_with_arguments_then_arguments_are_ignored()
    {
        using var dbContext = _sut.CreateDbContext(["--some-arg", "value"]);

        dbContext.ShouldNotBeNull();
    }
}
