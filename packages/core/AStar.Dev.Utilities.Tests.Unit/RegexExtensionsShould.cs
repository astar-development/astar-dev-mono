namespace AStar.Dev.Utilities;

public class RegexExtensionsShould
{
    [Theory]
    [InlineData("",    false)]
    [InlineData("AAA", false)]
    [InlineData("123", false)]
    [InlineData("a",   true)]
    [InlineData("AaA", true)]
    [InlineData("12a", true)]
    public void ContainTheContainsAtLeastOneLowercaseLetterExtensionReturningTheExpectedResponse(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneLowercaseLetter().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("",    false)]
    [InlineData("123", false)]
    [InlineData("a",   false)]
    [InlineData("AAA", true)]
    [InlineData("aaA", true)]
    [InlineData("12A", true)]
    public void ContainTheContainsAtLeastOneUppercaseLetterExtensionReturningTheExpectedResponse(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneUppercaseLetter().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("",    false)]
    [InlineData("a",   false)]
    [InlineData("AAA", false)]
    [InlineData("123", true)]
    [InlineData("aa1", true)]
    [InlineData("12A", true)]
    public void ContainTheContainsAtLeastOneDigitExtensionReturningTheExpectedResponse(string sut, bool expectedResponse)
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
    public void ContainTheContainsAtLeastOneSpecialCharacterExtensionReturningTheExpectedResponse(string sut, bool expectedResponse)
        => sut.ContainsAtLeastOneSpecialCharacter().ShouldBe(expectedResponse);
}