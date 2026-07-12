using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Services;

public sealed class GivenADatabaseInitializationService : IAsyncLifetime
{
    private SqliteConnection connection = null!;
    private DbContextOptions<AppDbContext> options = null!;

    public async ValueTask InitializeAsync()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();

    [Fact]
    public async Task when_initialising_with_a_cancellation_token_then_the_token_is_passed_to_the_context_factory()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var sut = new DatabaseInitializationService(contextFactory, new LoggerConfiguration().CreateLogger());
        using var tokenSource = new CancellationTokenSource();

        await sut.InitialiseAsync(tokenSource.Token);

        await contextFactory.Received(1).CreateDbContextAsync(tokenSource.Token);
    }

    [Fact]
    public async Task when_initialising_with_a_cancelled_token_then_an_operation_canceled_exception_is_thrown()
    {
        var contextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
        var sut = new DatabaseInitializationService(contextFactory, new LoggerConfiguration().CreateLogger());
        using var tokenSource = new CancellationTokenSource();
        await tokenSource.CancelAsync();

        var exception = await Record.ExceptionAsync(() => sut.InitialiseAsync(tokenSource.Token));

        exception.ShouldBeAssignableTo<OperationCanceledException>();
    }
}
