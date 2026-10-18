using AStarDev.EFCoreExtensions.TestsUnit.Support;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.EFCoreExtensions.TestsUnit;

public sealed class GeneratedIntId_should : IDisposable
{
    private readonly SqliteFixture<SqliteDefaultContext> fixture = new();

    public void Dispose() => fixture.Dispose();

    [Fact]
    public void generate_an_id_when_the_id_is_empty()
    {
        using var context = fixture.CreateContext();
        var entity = new IntEntity { Name = "one" };
        context.Ints.Add(entity);

        context.SaveChanges();

        entity.Id.ShouldNotBe(IntId.Empty);
        entity.Id.Value.ShouldBe(1);
    }

    [Fact]
    public void generate_increasing_unique_ids()
    {
        using var context = fixture.CreateContext();
        var first = new IntEntity { Name = "a" };
        var second = new IntEntity { Name = "b" };
        context.Ints.AddRange(first, second);

        context.SaveChanges();

        second.Id.Value.ShouldBeGreaterThan(first.Id.Value);
    }

    [Fact]
    public void keep_an_explicitly_supplied_id()
    {
        using (var context = fixture.CreateContext())
        {
            context.Ints.Add(new IntEntity { Id = new IntId(500), Name = "explicit" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        verify.Ints.Single().Id.ShouldBe(new IntId(500));
    }

    [Fact]
    public void persist_and_reload_the_entity_by_its_id()
    {
        IntId id;
        using (var context = fixture.CreateContext())
        {
            var entity = new IntEntity { Name = "reload" };
            context.Ints.Add(entity);
            context.SaveChanges();
            id = entity.Id;
        }

        using var verify = fixture.CreateContext();
        verify.Ints.Single(e => e.Id == id).Name.ShouldBe("reload");
        verify.Ints.Find(id)!.Name.ShouldBe("reload");
    }

    [Fact]
    public void not_reuse_ids_after_the_latest_row_is_deleted_on_sqlite()
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

        second.Id.Value.ShouldBeGreaterThan(first.Id.Value);
    }

    [Fact]
    public void use_the_entity_id_property_as_the_primary_key()
    {
        using var context = fixture.CreateContext();

        var key = context.Model.FindEntityType(typeof(IntEntity))!.FindPrimaryKey()!;

        key.Properties.Select(p => p.Name).ShouldBe([nameof(IntEntity.Id)]);
    }

    [Fact]
    public void return_the_property_builder_for_further_configuration()
    {
        var builder = new ModelBuilder();

        var result = builder.Entity<IntEntity>().HasGeneratedIntId(e => e.Id);

        result.ShouldNotBeNull();
        result.Metadata.Name.ShouldBe(nameof(IntEntity.Id));
    }
}
