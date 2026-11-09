using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenARemoteItemIdFactory
{
    [Fact]
    public void when_an_id_is_created_then_it_is_trimmed()
    {
        var created = RemoteItemIdFactory.Create("  01ABCDEF!123  ");

        created.Match(id => id.Value, error => error.Message).ShouldBe("01ABCDEF!123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_id_is_blank_then_it_is_rejected(string? value)
    {
        var created = RemoteItemIdFactory.Create(value);

        created.Match(_ => "created", _ => "rejected").ShouldBe("rejected");
    }
}
