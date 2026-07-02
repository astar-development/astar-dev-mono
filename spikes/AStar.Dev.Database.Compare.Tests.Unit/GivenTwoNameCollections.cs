namespace AStar.Dev.Database.Compare.Tests.Unit;

public class GivenTwoNameCollections
{
    [Fact]
    public void when_name_exists_in_reference_with_different_case_then_excluded_from_result()
    {
        var namesToCheck = new List<(string, bool)> { ("Action", false) };
        var referenceNames = new List<(string, bool)> { ("action", false) };

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBeEmpty();
    }

    [Fact]
    public void when_name_does_not_exist_in_reference_then_included_in_result()
    {
        var namesToCheck = new List<(string, bool)> { ("Cosplay", false) };
        var referenceNames = new List<(string, bool)> { ("Action", true) };

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBe(namesToCheck);
    }

    [Fact]
    public void when_reference_is_empty_then_all_names_are_missing()
    {
        var namesToCheck = new List<(string, bool)> { ("Cosplay", true), ("Action", false) };
        var referenceNames = new List<(string, bool)>();

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBe(namesToCheck.Where(n => n.Item2));
    }

    [Fact]
    public void when_names_to_check_is_empty_then_result_is_empty()
    {
        var namesToCheck = new List<(string, bool)>();
        var referenceNames = new List<(string, bool)> { ("Action", true) };

        var missingNames = MissingCategoryFinder.FindMissing(namesToCheck, referenceNames);

        missingNames.ShouldBeEmpty();
    }
}
