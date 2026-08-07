using Demo.Infrastructure;

namespace Demo;

public abstract class UseCaseBase(DatabaseDbContextFactory dbContextFactory)
{
    public async IAsyncEnumerable<string> Execute()
    {
        using DatabaseDbContext dbContext = dbContextFactory.CreateDbContext();

        await foreach (string result in ExecuteCase(dbContext))
        {
            yield return result;
        }
    }

    protected abstract IAsyncEnumerable<string> ExecuteCase(DatabaseDbContext dbContext);
}