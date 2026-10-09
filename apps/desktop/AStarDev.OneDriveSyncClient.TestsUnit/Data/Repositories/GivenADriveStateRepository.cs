using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb;
using AStarDev.OneDriveSyncClient.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AccountId = AStar.Dev.Infrastructure.AppDb.Entities.AccountId;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Data.Repositories;

public sealed class GivenADriveStateRepository : IDisposable
{
    private static readonly AccountId UserOne = new("user-1");

    private readonly SqliteConnection connection;
    private readonly AppDbContext seedingContext;
    private readonly IDbContextFactory<AppDbContext> factory;
    private bool disposed;

    public GivenADriveStateRepository()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        seedingContext = new AppDbContext(options);
        _ = seedingContext.Database.EnsureCreated();
        _ = seedingContext.Accounts.Add(new AccountEntity { Id = UserOne });
        _ = seedingContext.SaveChanges();

        factory = Substitute.For<IDbContextFactory<AppDbContext>>();
        factory.CreateDbContextAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(new AppDbContext(options)));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        seedingContext.Dispose();
        connection.Dispose();
    }

    [Fact]
    public async Task when_a_new_drive_state_is_upserted_then_rules_fingerprint_and_last_full_enumeration_are_persisted()
    {
        var sut = new DriveStateRepository(factory);
        var enumeratedAt = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);

        await sut.UpsertAsync(new DriveStateEntity { AccountId = UserOne, RulesFingerprint = Option.Some("fp-1"), LastFullEnumerationAt = Option.Some(enumeratedAt) }, TestContext.Current.CancellationToken);

        var stored = (await sut.GetByAccountIdAsync(UserOne, TestContext.Current.CancellationToken)).ShouldBeOfType<Option<DriveStateEntity>.Some>().Value;
        stored.RulesFingerprint.ShouldBe(Option.Some("fp-1"));
        stored.LastFullEnumerationAt.ShouldBe(Option.Some(enumeratedAt));
    }

    [Fact]
    public async Task when_an_existing_drive_state_is_upserted_then_rules_fingerprint_and_last_full_enumeration_are_updated()
    {
        var sut = new DriveStateRepository(factory);
        await sut.UpsertAsync(new DriveStateEntity { AccountId = UserOne, RulesFingerprint = Option.Some("fp-1") }, TestContext.Current.CancellationToken);
        var enumeratedAt = new DateTimeOffset(2026, 10, 9, 13, 0, 0, TimeSpan.Zero);

        await sut.UpsertAsync(new DriveStateEntity { AccountId = UserOne, RulesFingerprint = Option.Some("fp-2"), LastFullEnumerationAt = Option.Some(enumeratedAt) }, TestContext.Current.CancellationToken);

        var stored = (await sut.GetByAccountIdAsync(UserOne, TestContext.Current.CancellationToken)).ShouldBeOfType<Option<DriveStateEntity>.Some>().Value;
        stored.RulesFingerprint.ShouldBe(Option.Some("fp-2"));
        stored.LastFullEnumerationAt.ShouldBe(Option.Some(enumeratedAt));
    }

    [Fact]
    public async Task when_a_drive_state_is_upserted_without_the_new_fields_then_they_are_none()
    {
        var sut = new DriveStateRepository(factory);

        await sut.UpsertAsync(new DriveStateEntity { AccountId = UserOne }, TestContext.Current.CancellationToken);

        var stored = (await sut.GetByAccountIdAsync(UserOne, TestContext.Current.CancellationToken)).ShouldBeOfType<Option<DriveStateEntity>.Some>().Value;
        stored.RulesFingerprint.ShouldBe(Option.None<string>());
        stored.LastFullEnumerationAt.ShouldBe(Option.None<DateTimeOffset>());
    }
}
