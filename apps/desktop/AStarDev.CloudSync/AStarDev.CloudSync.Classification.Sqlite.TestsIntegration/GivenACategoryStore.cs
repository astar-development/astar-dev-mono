using static AStarDev.CloudSync.Classification.Sqlite.TestsIntegration.ClassificationData;

namespace AStarDev.CloudSync.Classification.Sqlite.TestsIntegration;

public sealed class GivenACategoryStore : MigratedDatabaseTests
{
    [Fact]
    public async Task when_no_category_was_added_then_none_are_read()
    {
        var all = Ok(await Database.Categories.GetAllAsync(CancellationToken));

        all.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_category_is_upserted_then_it_is_returned_and_read_back()
    {
        var category = Category("Colours");

        var stored = Ok(await Database.Categories.UpsertAsync(category, CancellationToken));

        stored.ShouldBe(category);
        Ok(await Database.Categories.GetAllAsync(CancellationToken)).ShouldBe([category]);
    }

    [Fact]
    public async Task when_a_category_with_the_same_name_in_another_case_is_upserted_then_the_stored_one_is_returned()
    {
        var first = Ok(await Database.Categories.UpsertAsync(Category("Colours"), CancellationToken));

        var second = Ok(await Database.Categories.UpsertAsync(Category("COLOURS"), CancellationToken));

        (second, Ok(await Database.Categories.GetAllAsync(CancellationToken)).Count).ShouldBe((first, 1));
    }

    [Fact]
    public async Task when_a_category_name_differs_only_by_a_non_ascii_letter_case_then_the_stored_one_is_returned()
    {
        var first = Ok(await Database.Categories.UpsertAsync(Category("Ärger"), CancellationToken));

        var second = Ok(await Database.Categories.UpsertAsync(Category("ärger"), CancellationToken));

        second.ShouldBe(first);
    }

    [Fact]
    public async Task when_the_same_category_is_upserted_by_twenty_parallel_calls_then_one_category_is_stored_and_all_calls_return_it()
    {
        var results = await InParallelAsync(20, async index => Ok(await Database.Categories.UpsertAsync(Category(index % 2 == 0 ? "Colours" : "colours"), CancellationToken)));

        (results.Select(category => category.Id).Distinct().Count(), Ok(await Database.Categories.GetAllAsync(CancellationToken)).Count).ShouldBe((1, 1));
    }

    [Fact]
    public async Task when_the_service_restarts_then_the_categories_are_still_there()
    {
        var category = Ok(await Database.Categories.UpsertAsync(Category("Colours"), CancellationToken));

        await Database.RestartAsync();

        Ok(await Database.Categories.GetAllAsync(CancellationToken)).ShouldBe([category]);
    }
}
