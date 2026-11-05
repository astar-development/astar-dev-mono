using AStarDev.CloudSync.Domain.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Domain.TestsUnit.Accounts;

public class GivenAProviderAccountKeyFactory
{
    private const string HomeAccountIdentifier = "00000000-0000-0000-a1b2-c3d4e5f60718.9188040d-6c67-4c5b-b112-36a304b66dad";

    [Fact]
    public void when_created_with_an_identifier_then_the_key_holds_it()
    {
        var keyValue = ProviderAccountKeyFactory.Create(HomeAccountIdentifier).Match(key => key.Value, error => error.Message);

        keyValue.ShouldBe(HomeAccountIdentifier);
    }

    [Fact]
    public void when_created_with_surrounding_whitespace_then_the_key_is_trimmed()
    {
        var keyValue = ProviderAccountKeyFactory.Create($"  {HomeAccountIdentifier}\t").Match(key => key.Value, error => error.Message);

        keyValue.ShouldBe(HomeAccountIdentifier);
    }

    [Fact]
    public void when_two_identifiers_differ_only_by_case_then_the_keys_are_equal()
    {
        var lowerCaseKey = ProviderAccountKeyFactory.Create(HomeAccountIdentifier);
        var upperCaseKey = ProviderAccountKeyFactory.Create(HomeAccountIdentifier.ToUpperInvariant());

        upperCaseKey.ShouldBe(lowerCaseKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_missing_identifier_then_creation_fails(string? missingIdentifier)
    {
        var succeeded = ProviderAccountKeyFactory.Create(missingIdentifier).Match(_ => true, _ => false);

        succeeded.ShouldBeFalse();
    }
}
