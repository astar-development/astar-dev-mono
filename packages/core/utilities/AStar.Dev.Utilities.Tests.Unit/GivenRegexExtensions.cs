namespace AStar.Dev.Utilities.Tests.Unit;

public class GivenRegexExtensions
{
    [Theory]
    [InlineData("",    false)]
    [InlineData("AAA", false)]
    [InlineData("123", false)]
    [InlineData("a",   true)]
    [InlineData("AaA", true)]
    [InlineData("12a", true)]
    public void when_contains_at_least_one_lowercase_letter_is_called_then_returns_the_expected_response(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneLowercaseLetter().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("",    false)]
    [InlineData("123", false)]
    [InlineData("a",   false)]
    [InlineData("AAA", true)]
    [InlineData("aaA", true)]
    [InlineData("12A", true)]
    public void when_contains_at_least_one_uppercase_letter_is_called_then_returns_the_expected_response(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneUppercaseLetter().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("",    false)]
    [InlineData("a",   false)]
    [InlineData("AAA", false)]
    [InlineData("123", true)]
    [InlineData("aa1", true)]
    [InlineData("12A", true)]
    public void when_contains_at_least_one_digit_is_called_then_returns_the_expected_response(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneDigit().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("",      false)]
    [InlineData("a",     false)]
    [InlineData("AAA",   false)]
    [InlineData("123[",  true)]
    [InlineData("aa1!",  true)]
    [InlineData("12A-",  true)]
    [InlineData("12A/",  true)]
    [InlineData(@"12A\", true)]
    [InlineData("12A:",  true)]
    [InlineData("12A@",  true)]
    [InlineData("12A`",  true)]
    [InlineData("12A{",  true)]
    [InlineData("12A}",  true)]
    [InlineData("12A¬",  true)]
    [InlineData("12A#",  true)]
    [InlineData("12A~",  true)]
    public void when_contains_at_least_one_special_character_is_called_then_returns_the_expected_response(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneSpecialCharacter().ShouldBe(expectedResponse);
}