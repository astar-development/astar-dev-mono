using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenAChangeCursorFactory
{
    private const string DeltaLink = "https://graph.microsoft.com/v1.0/me/drive/root/delta?token=secret-delta-token";

    [Fact]
    public void when_a_cursor_is_created_then_it_keeps_the_delta_link()
    {
        var created = ChangeCursorFactory.Create($" {DeltaLink} ");

        created.Match(cursor => cursor.Value, error => error.Message).ShouldBe(DeltaLink);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void when_the_cursor_is_blank_then_it_is_rejected(string? value)
    {
        var created = ChangeCursorFactory.Create(value);

        created.Match(_ => "created", _ => "rejected").ShouldBe("rejected");
    }

    [Fact]
    public void when_a_cursor_is_printed_then_its_token_is_redacted()
    {
        var cursor = RemoteIds.Cursor(DeltaLink);

        cursor.ToString().ShouldNotContain("secret-delta-token");
    }
}
