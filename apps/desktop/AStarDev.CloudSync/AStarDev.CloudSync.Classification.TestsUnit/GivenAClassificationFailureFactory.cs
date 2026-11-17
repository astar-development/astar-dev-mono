namespace AStarDev.CloudSync.Classification.TestsUnit;

public class GivenAClassificationFailureFactory
{
    [Fact]
    public void when_each_case_is_created_then_it_is_that_case()
    {
        var failures = new[]
        {
            ClassificationFailureFactory.CreateNotFound(),
            ClassificationFailureFactory.CreateFileNotFound(),
            ClassificationFailureFactory.CreateUnavailable(),
            ClassificationFailureFactory.CreateInvalidSeed(),
        };

        failures.Select(failure => failure.GetType().Name).ShouldBe(["NotFound", "FileNotFound", "Unavailable", "InvalidSeed"]);
    }

    [Fact]
    public void when_a_missing_term_and_a_missing_file_are_created_then_they_are_not_equal()
    {
        ClassificationFailureFactory.CreateFileNotFound().ShouldNotBe(ClassificationFailureFactory.CreateNotFound());
    }
}
