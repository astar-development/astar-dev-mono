using AStarDev.EFCoreExtensions.TestsUnit.Support;

namespace AStarDev.EFCoreExtensions.TestsUnit;

public sealed class DatabaseProvider_should : IDisposable
{
    private readonly SqliteFixture<OtherProviderContext> fixture = new();

    public void Dispose() => fixture.Dispose();

    [Fact]
    public void still_generate_int_ids_when_not_targeting_sqlite()
    {
        using var context = fixture.CreateContext();
        var entity = new IntEntity { Name = "a" };
        context.Ints.Add(entity);

        context.SaveChanges();

        entity.Id.ShouldNotBe(IntId.Empty);
    }

    [Fact]
    public void still_generate_long_ids_when_not_targeting_sqlite()
    {
        using var context = fixture.CreateContext();
        var entity = new LongEntity { Name = "a" };
        context.Longs.Add(entity);

        context.SaveChanges();

        entity.Id.ShouldNotBe(LongId.Empty);
    }

    [Fact]
    public void not_request_sqlite_autoincrement_when_not_targeting_sqlite()
    {
        using var context = fixture.CreateContext();
        var first = new IntEntity { Name = "a" };
        context.Ints.Add(first);
        context.SaveChanges();
        context.Ints.Remove(first);
        context.SaveChanges();

        var second = new IntEntity { Name = "b" };
        context.Ints.Add(second);
        context.SaveChanges();

        // Without AUTOINCREMENT, SQLite reuses the highest rowid once that row is gone.
        second.Id.ShouldBe(first.Id);
    }
}
