using AStarDev.EFCoreExtensions.TestsUnit.Support;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.EFCoreExtensions.TestsUnit;

public sealed class StringId_should : IDisposable
{
    private readonly SqliteFixture<SqliteDefaultContext> fixture = new();

    public void Dispose() => fixture.Dispose();

    [Fact]
    public void persist_the_application_supplied_id()
    {
        using (var context = fixture.CreateContext())
        {
            context.Strings.Add(new StringEntity { Id = new StringId("abc"), Name = "one" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var restored = verify.Strings.Single();
        restored.Id.ShouldBe(new StringId("abc"));
        restored.Name.ShouldBe("one");
    }

    [Fact]
    public void find_by_id()
    {
        using (var context = fixture.CreateContext())
        {
            context.Strings.AddRange(
                new StringEntity { Id = new StringId("a"), Name = "first" },
                new StringEntity { Id = new StringId("b"), Name = "second" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        verify.Strings.Find(new StringId("b"))!.Name.ShouldBe("second");
    }

    [Fact]
    public void reject_a_duplicate_id()
    {
        using var context = fixture.CreateContext();
        context.Strings.Add(new StringEntity { Id = new StringId("dup"), Name = "one" });
        context.SaveChanges();

        using var second = fixture.CreateContext();
        second.Strings.Add(new StringEntity { Id = new StringId("dup"), Name = "two" });

        Should.Throw<DbUpdateException>(() => second.SaveChanges());
    }

    [Fact]
    public void update_an_entity_without_changing_its_id()
    {
        using (var context = fixture.CreateContext())
        {
            context.Strings.Add(new StringEntity { Id = new StringId("k"), Name = "old" });
            context.SaveChanges();
        }

        using (var update = fixture.CreateContext())
        {
            update.Strings.Single().Name = "new";
            update.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        verify.Strings.Single().Name.ShouldBe("new");
    }
}
