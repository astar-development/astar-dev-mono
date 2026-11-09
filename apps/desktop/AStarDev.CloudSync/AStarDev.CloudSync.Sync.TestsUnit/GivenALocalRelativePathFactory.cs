using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenALocalRelativePathFactory
{
    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("Photos", "Photos")]
    [InlineData("Photos/2026", "Photos/2026")]
    [InlineData("Photos\\2026", "Photos/2026")]
    [InlineData("Photos/", "Photos")]
    public void when_the_path_is_relative_then_it_is_normalised(string value, string expected)
    {
        var result = LocalRelativePathFactory.Create(value);

        result.Match(path => path.Value, _ => "invalid").ShouldBe(expected);
    }

    [Fact]
    public void when_the_value_is_missing_then_it_is_the_root()
    {
        var result = LocalRelativePathFactory.Create(null);

        result.Match(path => path, _ => throw new InvalidOperationException()).ShouldBe(LocalRelativePathFactory.Root);
    }

    [Theory]
    [InlineData("../")]
    [InlineData("..")]
    [InlineData("a/../../b")]
    [InlineData("a/..")]
    [InlineData("/etc")]
    [InlineData("\\server\\share")]
    [InlineData("C:\\")]
    [InlineData("C:/Users")]
    [InlineData("a/./b")]
    public void when_the_path_is_absolute_or_leaves_the_root_then_it_is_invalid(string value)
    {
        var result = LocalRelativePathFactory.Create(value);

        result.Match(path => (object)path, error => error).ShouldBe(LocalFolderErrorFactory.CreateInvalidPath());
    }

    [Fact]
    public void when_paths_differ_only_in_case_then_they_are_equal()
    {
        var lower = LocalRelativePathFactory.Create("photos/2026").Match(path => path, _ => throw new InvalidOperationException());
        var upper = LocalRelativePathFactory.Create("PHOTOS/2026").Match(path => path, _ => throw new InvalidOperationException());

        (lower == upper, lower.GetHashCode() == upper.GetHashCode()).ShouldBe((true, true));
    }

    [Fact]
    public void when_the_path_is_composed_differently_then_it_is_normalised_to_the_composed_form()
    {
        var decomposed = LocalRelativePathFactory.Create("Cafe\u0301");
        var composed = LocalRelativePathFactory.Create("Caf\u00e9");

        decomposed.Match(path => path, _ => throw new InvalidOperationException()).ShouldBe(composed.Match(path => path, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void when_the_path_is_written_to_a_string_then_it_is_redacted()
    {
        var path = LocalRelativePathFactory.Create("Private/Taxes").Match(created => created, _ => throw new InvalidOperationException());

        path.ToString().ShouldNotContain("Taxes");
    }
}
