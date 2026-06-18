namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAFileClassificationCategoryEntity
{
    [Fact]
    public void when_instantiated_then_name_defaults_to_empty_string() =>
        new FileClassificationCategoryEntity().Name.ShouldBe(string.Empty);

    [Fact]
    public void when_instantiated_then_level_defaults_to_zero() =>
        new FileClassificationCategoryEntity().Level.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_parent_id_is_null() =>
        new FileClassificationCategoryEntity().ParentId.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_parent_navigation_is_null() =>
        new FileClassificationCategoryEntity().Parent.ShouldBeNull();

    [Fact]
    public void when_parent_id_is_set_then_it_reflects_in_the_property()
    {
        // This is very slow...maybe
        var entity = new FileClassificationCategoryEntity
        {
            ParentId = 42
        };

        entity.ParentId.ShouldBe(42);
    }

    [Fact]
    public void when_name_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationCategoryEntity
        {
            Name = "Photos"
        };

        entity.Name.ShouldBe("Photos");
    }

    [Fact]
    public void when_level_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationCategoryEntity
        {
            Level = 2
        };

        entity.Level.ShouldBe(2);
    }

    [Fact]
    public void when_is_famous_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationCategoryEntity
        {
            IsFamous = true
        };

        entity.IsFamous.ShouldBe(true);
    }

    [Fact]
    public void when_is_internet_is_set_then_it_reflects_in_the_property()
    {
        var entity = new FileClassificationCategoryEntity
        {
            IsInternet = true
        };

        entity.IsInternet.ShouldBe(true);
    }
}
