namespace AStar.Dev.Utilities.Tests.Unit;

public class EncryptionExtensionsShould
{
    [Fact]
    public void EncryptAsExpected()
        => "irrelevant-string".Encrypt().ShouldBe("f4eR8dX1bWiwTF/GNd02dTl2wnPfj9jFHWi3ZVCxceQ=");

    [Fact]
    public void DecryptAsExpected()
        => "f4eR8dX1bWiwTF/GNd02dTl2wnPfj9jFHWi3ZVCxceQ=".Decrypt().ShouldBe("irrelevant-string");
}
