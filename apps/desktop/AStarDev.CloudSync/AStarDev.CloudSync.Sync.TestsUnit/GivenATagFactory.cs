using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenATagFactory
{
    [Fact]
    public void when_tags_and_hashes_are_created_then_they_are_trimmed()
    {
        var values = (ContentTagFactory.Create(" {ctag,1} ").Match(tag => tag.Value, error => error.Message), EntityTagFactory.Create(" \"{etag},2\" ").Match(tag => tag.Value, error => error.Message), ContentHashFactory.Create(" aGFzaA== ").Match(hash => hash.Value, error => error.Message));

        values.ShouldBe(("{ctag,1}", "\"{etag},2\"", "aGFzaA=="));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_a_tag_or_hash_is_blank_then_it_is_rejected(string? value)
    {
        var results = new[] { ContentTagFactory.Create(value).Match(_ => "created", _ => "rejected"), EntityTagFactory.Create(value).Match(_ => "created", _ => "rejected"), ContentHashFactory.Create(value).Match(_ => "created", _ => "rejected") };

        results.ShouldAllBe(result => result == "rejected");
    }
}
