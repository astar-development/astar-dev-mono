using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncedItemSearchCriteriaFactory
{
    private static readonly AccountId TestAccountId = new("acc-1");

    [Fact]
    public void when_no_sort_order_specified_then_sort_order_defaults_to_name_ascending()
    {
        var criteria = SyncedItemSearchCriteriaFactory.Create(TestAccountId);

        criteria.SortOrder.ShouldBe(SearchSortOrder.NameAscending);
    }

    [Fact]
    public void when_name_descending_sort_order_specified_then_sort_order_is_name_descending()
    {
        var criteria = SyncedItemSearchCriteriaFactory.Create(TestAccountId, sortOrder: SearchSortOrder.NameDescending);

        criteria.SortOrder.ShouldBe(SearchSortOrder.NameDescending);
    }

    [Fact]
    public void when_size_ascending_sort_order_specified_then_sort_order_is_size_ascending()
    {
        var criteria = SyncedItemSearchCriteriaFactory.Create(TestAccountId, sortOrder: SearchSortOrder.SizeAscending);

        criteria.SortOrder.ShouldBe(SearchSortOrder.SizeAscending);
    }

    [Fact]
    public void when_size_descending_sort_order_specified_then_sort_order_is_size_descending()
    {
        var criteria = SyncedItemSearchCriteriaFactory.Create(TestAccountId, sortOrder: SearchSortOrder.SizeDescending);

        criteria.SortOrder.ShouldBe(SearchSortOrder.SizeDescending);
    }
}
