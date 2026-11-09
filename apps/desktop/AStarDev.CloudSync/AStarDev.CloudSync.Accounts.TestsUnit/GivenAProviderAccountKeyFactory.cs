using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Accounts.TestsUnit;

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
    public void when_created_with_mixed_case_then_the_key_keeps_its_case()
    {
        var mixedCaseIdentifier = "00000000-0000-0000-A1B2-c3d4e5f60718.9188040D-6c67-4c5b-b112-36a304b66dad";

        var keyValue = ProviderAccountKeyFactory.Create(mixedCaseIdentifier).Match(key => key.Value, error => error.Message);

        keyValue.ShouldBe(mixedCaseIdentifier);
    }

    [Fact]
    public void when_the_key_is_converted_to_text_then_the_identifier_is_not_shown()
    {
        var keyText = ProviderAccountKeyFactory.Create(HomeAccountIdentifier).Match(key => key.ToString(), error => error.Message);

        keyText.ShouldBe("ProviderAccountKey(redacted)");
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
