namespace AStar.Dev.Utilities;

public sealed class StringExtensionsShould
{
    private const string AnyJson          = "{\"AnyInt\":0,\"AnyString\":\"\"}";
    private const string NotNullString    = "value does not matter";
    private const string WhitespaceString = " ";
#pragma warning disable CA1805
    private readonly string? nullString = null;
#pragma warning restore CA1805

    [Fact]
    public void ContainTheIsNullMethodWhichReturnsTheResult() =>
        nullString.IsNull().ShouldBeTrue();

    [Fact]
    public void ContainTheIsNotNullMethodWhichReturnsTheResult() =>
        NotNullString.IsNotNull().ShouldBeTrue();

    [Fact]
    public void ContainTheIsNullOrWhiteSpaceMethodWhichReturnsTheResult() =>
        WhitespaceString.IsNullOrWhiteSpace().ShouldBeTrue();

    [Fact]
    public void ContainTheIsNotNullOrWhiteSpaceMethodWhichReturnsTheResult() =>
        NotNullString.IsNotNullOrWhiteSpace().ShouldBeTrue();

    [Fact]
    public void ContainTheFromJsonMethodWhichReturnsTheResult() =>
        AnyJson.FromJson<AnyClass>().ShouldBeEquivalentTo(new AnyClass());

    [Fact]
    public void ContainTheFromJsonTakingJsonSerializerOptionsMethodWhichReturnsTheResult() =>
        AnyJson.FromJson<AnyClass>(new()).ShouldBeEquivalentTo(new AnyClass());

    [Theory]
    [InlineData("no-Extension",           false)]
    [InlineData("Wrong-Extension.txt",    false)]
    [InlineData("Wrong-Extension.DOC",    false)]
    [InlineData("Wrong-Extension.PdF",    false)]
    [InlineData("Correct-Extension.jpG",  true)]
    [InlineData("Correct-Extension.jpeG", true)]
    [InlineData("Correct-Extension.bmp",  true)]
    [InlineData("Write-Extension.png",    true)]
    [InlineData("Correct-Extension.gif",  true)]
    public void ContainTheIsImageExtensionReturningTheExpectedResults(string fileName, bool expectedResponse) =>
        fileName.IsImage().ShouldBe(expectedResponse);

    [Theory]
    [InlineData("no-Truncation", 20, "no-Truncation")]
    [InlineData("Small-String-Truncation.txt", 10, "Small-Stri")]
    [InlineData("Small-String-Truncation.DOC", 15, "Small-String-Tr")]
    [InlineData("Small-String-Truncation.PdF", 20, "Small-String-Truncat")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.jpG", 100,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-Str")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.jpeG", 120,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Lar")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.bmp", 140,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-Stri")]
    [InlineData("Write-Truncation.png", 10, "Write-Trun")]
    [InlineData("Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Truncation.gif", 160,
                "Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String-Large-String--Tru")]
    public void ContainTheTruncateIfRequiredReturningTheExpectedResults(string fileName, int truncateLength, string expectedResponse) =>
        fileName.TruncateIfRequired(truncateLength).ShouldBe(expectedResponse);

    [Theory]
    [InlineData("no-number",                   false)]
    [InlineData("number-at-the-end-123",       false)]
    [InlineData("123-number-at-the-beginning", false)]
    [InlineData("number-in-the-123-middle",    false)]
    [InlineData("1",                           true)]
    [InlineData("12",                          true)]
    [InlineData("123456",                      true)]
    public void ContainTheIsNumberOnlyExtensionReturningTheExpectedResults(string fileName, bool expectedResponse) =>
        fileName.IsNumberOnly().ShouldBe(expectedResponse);
}