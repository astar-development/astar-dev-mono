using AStar.Dev.Wallpaper.Scrapper.Models;
using AStar.Dev.Wallpaper.Scrapper.Support;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.Support;

public sealed class GivenTheCategoryMerger
{
    private static Category BuildCategory(string id, string name, int lastKnownImageCount, int lastPageVisited, int totalPages)
        => new() { Id = id, Name = name, LastKnownImageCount = lastKnownImageCount, LastPageVisited = lastPageVisited, TotalPages = totalPages, };

    [Fact]
    public void when_an_updated_category_matches_an_existing_category_then_it_appears_in_to_update()
    {
        (string Id, string Name, int LastKnownImageCount, int LastPageVisited, int TotalPages)[] existing = [("cat-1", "Cars", 1, 1, 1),];
        var updated = new[] { BuildCategory("cat-1", "Cars", 10, 2, 5), };

        var (toUpdate, _) = CategoryMerger.MergeCategories(existing, updated);

        toUpdate.ShouldHaveSingleItem().Id.ShouldBe("cat-1");
    }

    [Fact]
    public void when_an_updated_category_matches_an_existing_category_then_the_updated_counts_are_used()
    {
        (string Id, string Name, int LastKnownImageCount, int LastPageVisited, int TotalPages)[] existing = [("cat-1", "Cars", 1, 1, 1),];
        var updated = new[] { BuildCategory("cat-1", "Cars", 10, 2, 5), };

        var (toUpdate, _) = CategoryMerger.MergeCategories(existing, updated);

        var item = toUpdate.ShouldHaveSingleItem();
        item.LastKnownImageCount.ShouldBe(10);
        item.LastPageVisited.ShouldBe(2);
        item.TotalPages.ShouldBe(5);
    }

    [Fact]
    public void when_an_updated_category_matches_an_existing_category_then_it_does_not_appear_in_to_add()
    {
        (string Id, string Name, int LastKnownImageCount, int LastPageVisited, int TotalPages)[] existing = [("cat-1", "Cars", 1, 1, 1),];
        var updated = new[] { BuildCategory("cat-1", "Cars", 10, 2, 5), };

        var (_, toAdd) = CategoryMerger.MergeCategories(existing, updated);

        toAdd.ShouldBeEmpty();
    }

    [Fact]
    public void when_an_updated_category_does_not_match_any_existing_category_then_it_appears_in_to_add()
    {
        var updated = new[] { BuildCategory("cat-new", "Nature", 3, 0, 2), };

        var (_, toAdd) = CategoryMerger.MergeCategories([], updated);

        toAdd.ShouldHaveSingleItem().Id.ShouldBe("cat-new");
    }

    [Fact]
    public void when_an_updated_category_does_not_match_any_existing_category_then_the_name_is_carried_to_add()
    {
        var updated = new[] { BuildCategory("cat-new", "Nature", 3, 0, 2), };

        var (_, toAdd) = CategoryMerger.MergeCategories([], updated);

        toAdd.ShouldHaveSingleItem().Name.ShouldBe("Nature");
    }

    [Fact]
    public void when_an_updated_category_does_not_match_any_existing_category_then_it_does_not_appear_in_to_update()
    {
        var updated = new[] { BuildCategory("cat-new", "Nature", 3, 0, 2), };

        var (toUpdate, _) = CategoryMerger.MergeCategories([], updated);

        toUpdate.ShouldBeEmpty();
    }

    [Fact]
    public void when_updated_contains_duplicate_ids_then_only_the_first_occurrence_is_merged()
    {
        var updated = new[] { BuildCategory("cat-1", "First", 1, 1, 1), BuildCategory("cat-1", "Second", 99, 99, 99), };

        var (_, toAdd) = CategoryMerger.MergeCategories([], updated);

        toAdd.ShouldHaveSingleItem().Name.ShouldBe("First");
    }

    [Fact]
    public void when_updated_is_empty_then_to_update_is_empty()
    {
        (string Id, string Name, int LastKnownImageCount, int LastPageVisited, int TotalPages)[] existing = [("cat-1", "Cars", 1, 1, 1),];

        var (toUpdate, _) = CategoryMerger.MergeCategories(existing, []);

        toUpdate.ShouldBeEmpty();
    }

    [Fact]
    public void when_updated_is_empty_then_to_add_is_empty()
    {
        var (_, toAdd) = CategoryMerger.MergeCategories([], []);

        toAdd.ShouldBeEmpty();
    }
}
