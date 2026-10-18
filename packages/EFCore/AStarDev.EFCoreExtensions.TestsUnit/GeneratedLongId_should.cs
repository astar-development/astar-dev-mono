using AStarDev.EFCoreExtensions.TestsUnit.Support;

namespace AStarDev.EFCoreExtensions.TestsUnit;

public sealed class GeneratedLongId_should : IDisposable
{
    private readonly SqliteFixture<SqliteDefaultContext> fixture = new();

    public void Dispose() => fixture.Dispose();

    [Fact]
    public void generate_an_id_when_the_id_is_empty()
    {
        using var context = fixture.CreateContext();
        var entity = new LongEntity { Name = "one" };
        context.Longs.Add(entity);

        context.SaveChanges();

        entity.Id.Value.ShouldBe(1L);
    }

    [Fact]
    public void generate_increasing_unique_ids()
    {
        using var context = fixture.CreateContext();
        var first = new LongEntity { Name = "a" };
        var second = new LongEntity { Name = "b" };
        context.Longs.AddRange(first, second);

        context.SaveChanges();

        second.Id.Value.ShouldBeGreaterThan(first.Id.Value);
    }

    [Fact]
    public void round_trip_values_beyond_the_int_range()
    {
        var big = (long)int.MaxValue + 10;
        using (var context = fixture.CreateContext())
        {
            context.Longs.Add(new LongEntity { Id = new LongId(big), Name = "big" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        verify.Longs.Single().Id.Value.ShouldBe(big);
    }

    [Fact]
    public void query_by_id()
    {
        using var context = fixture.CreateContext();
        var entity = new LongEntity { Name = "find me" };
        context.Longs.Add(entity);
        context.SaveChanges();
        var id = entity.Id;

        using var verify = fixture.CreateContext();
        verify.Longs.Single(e => e.Id == id).Name.ShouldBe("find me");
    }
}
