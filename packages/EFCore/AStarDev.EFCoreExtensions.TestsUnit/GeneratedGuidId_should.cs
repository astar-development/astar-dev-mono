using AStarDev.EFCoreExtensions.TestsUnit.Support;

namespace AStarDev.EFCoreExtensions.TestsUnit;

public sealed class GeneratedGuidId_should : IDisposable
{
    private readonly SqliteFixture<SqliteDefaultContext> fixture = new();

    public void Dispose() => fixture.Dispose();

    [Fact]
    public void generate_a_non_empty_id_when_the_id_is_empty()
    {
        using var context = fixture.CreateContext();
        var entity = new GuidEntity { Name = "one" };
        context.Guids.Add(entity);

        context.SaveChanges();

        entity.Id.ShouldNotBe(GuidId.Empty);
    }

    [Fact]
    public void generate_unique_ids()
    {
        using var context = fixture.CreateContext();
        var first = new GuidEntity { Name = "a" };
        var second = new GuidEntity { Name = "b" };
        context.Guids.AddRange(first, second);

        context.SaveChanges();

        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void keep_an_explicitly_supplied_id()
    {
        var id = new GuidId(Guid.NewGuid());
        using (var context = fixture.CreateContext())
        {
            context.Guids.Add(new GuidEntity { Id = id, Name = "explicit" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        verify.Guids.Single().Id.ShouldBe(id);
    }

    [Fact]
    public void reload_the_entity_by_its_generated_id()
    {
        GuidId id;
        using (var context = fixture.CreateContext())
        {
            var entity = new GuidEntity { Name = "reload" };
            context.Guids.Add(entity);
            context.SaveChanges();
            id = entity.Id;
        }

        using var verify = fixture.CreateContext();
        verify.Guids.Single(e => e.Id == id).Name.ShouldBe("reload");
    }
}
