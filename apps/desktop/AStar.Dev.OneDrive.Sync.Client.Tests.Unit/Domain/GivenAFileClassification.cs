using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFileClassification
{
    [Fact]
    public void when_three_level_classification_then_tag_name_is_level3()
    {
        var classification = FileClassificationFactory.Create("Subject", Option.Some("Vehicle"), Option.Some("Car"), false);

        classification.TagName.ShouldBe("Car");
    }

    [Fact]
    public void when_two_level_classification_then_tag_name_is_level2()
    {
        var classification = FileClassificationFactory.Create("Colour", Option.Some("Red"), Option.None<string>(), false);

        classification.TagName.ShouldBe("Red");
    }

    [Fact]
    public void when_one_level_classification_then_tag_name_is_level1()
    {
        var classification = FileClassificationFactory.Create("Archive", Option.None<string>(), Option.None<string>(), false);

        classification.TagName.ShouldBe("Archive");
    }

    [Fact]
    public void when_unclassified_sentinel_created_then_tag_name_is_unclassified()
    {
        var classification = FileClassificationFactory.CreateUnclassified();

        classification.TagName.ShouldBe("Unclassified");
        classification.IsSpecial.ShouldBeFalse();
    }
}
