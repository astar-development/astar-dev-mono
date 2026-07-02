namespace AStar.Dev.Database.Compare.Tests.Unit;

public class GivenTwoNameCollections
{
    [Fact]
    public void when_name_exists_in_reference_with_different_case_then_excluded_from_result()
    {
        var namesToCheck = new List<string> { "Action" };
        var referenceNames = new List<string> { "action" };

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBeEmpty();
    }

    [Fact]
    public void when_name_does_not_exist_in_reference_then_included_in_result()
    {
        var namesToCheck = new List<string> { "Cosplay" };
        var referenceNames = new List<string> { "Action" };

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBe(namesToCheck);
    }

    [Fact]
    public void when_reference_is_empty_then_all_names_are_missing()
    {
        var namesToCheck = new List<string> { "Cosplay", "Action" };
        var referenceNames = new List<string>();

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBe(namesToCheck);
    }

    [Fact]
    public void when_names_to_check_is_empty_then_result_is_empty()
    {
        var namesToCheck = new List<string>();
        var referenceNames = new List<string> { "Action" };

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBeEmpty();
    }
}
