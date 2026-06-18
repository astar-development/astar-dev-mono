namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenASyncedItemFileClassificationEntity
{
    [Fact]
    public void when_instantiated_then_id_defaults_to_zero() =>
        new SyncedItemFileClassificationEntity().Id.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_synced_item_id_defaults_to_zero() =>
        new SyncedItemFileClassificationEntity().SyncedItemId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_category_id_defaults_to_zero() =>
        new SyncedItemFileClassificationEntity().CategoryId.ShouldBe(0);

    [Fact]
    public void when_instantiated_then_synced_item_navigation_is_null() =>
        new SyncedItemFileClassificationEntity().SyncedItem.ShouldBeNull();

    [Fact]
    public void when_instantiated_then_category_navigation_is_null() =>
        new SyncedItemFileClassificationEntity().Category.ShouldBeNull();

    [Fact]
    public void when_synced_item_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new SyncedItemFileClassificationEntity
        {
            SyncedItemId = 42
        };

        entity.SyncedItemId.ShouldBe(42);
    }

    [Fact]
    public void when_category_id_is_set_then_it_reflects_in_the_property()
    {
        var entity = new SyncedItemFileClassificationEntity
        {
            CategoryId = 7
        };

        entity.CategoryId.ShouldBe(7);
    }
}
