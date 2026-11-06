namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed class GivenALogHash
{
    [Fact]
    public void when_a_value_is_hashed_then_the_result_is_sixteen_lower_case_hex_characters()
        => LogHash.Of("/home/ada/OneDrive/plan.docx").ShouldMatch("^[0-9a-f]{16}$");

    [Fact]
    public void when_the_same_value_is_hashed_twice_then_both_hashes_match()
        => LogHash.Of("/home/ada/OneDrive/plan.docx").ShouldBe(LogHash.Of("/home/ada/OneDrive/plan.docx"));

    [Fact]
    public void when_two_values_differ_only_by_case_then_their_hashes_differ()
        => LogHash.Of("/home/ada/Plan.docx").ShouldNotBe(LogHash.Of("/home/ada/plan.docx"));
}
