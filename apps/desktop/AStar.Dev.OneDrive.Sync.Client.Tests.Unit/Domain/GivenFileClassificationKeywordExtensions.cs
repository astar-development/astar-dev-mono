using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenFileClassificationKeywordExtensions
{
    private const string AnyValidValue = "cats";

    [Fact]
    public void when_override_is_true_and_classification_is_not_special_then_result_is_true()
    {
        var keyword = new FileClassificationKeyword(AnyValidValue, Option.Some(true));
        var classification = FileClassificationFactory.Create("Archive", Option.None<string>(), Option.None<string>(), false);

        var result = keyword.ResolveIsSpecial(classification);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_override_is_false_and_classification_is_special_then_result_is_false()
    {
        var keyword = new FileClassificationKeyword(AnyValidValue, Option.Some(false));
        var classification = FileClassificationFactory.Create("Archive", Option.None<string>(), Option.None<string>(), true);

        var result = keyword.ResolveIsSpecial(classification);

        result.ShouldBeFalse();
    }

    [Fact]
    public void when_override_is_none_and_classification_is_special_then_result_is_true()
    {
        var keyword = new FileClassificationKeyword(AnyValidValue, Option.None<bool>());
        var classification = FileClassificationFactory.Create("Archive", Option.None<string>(), Option.None<string>(), true);

        var result = keyword.ResolveIsSpecial(classification);

        result.ShouldBeTrue();
    }

    [Fact]
    public void when_override_is_none_and_classification_is_not_special_then_result_is_false()
    {
        var keyword = new FileClassificationKeyword(AnyValidValue, Option.None<bool>());
        var classification = FileClassificationFactory.Create("Archive", Option.None<string>(), Option.None<string>(), false);

        var result = keyword.ResolveIsSpecial(classification);

        result.ShouldBeFalse();
    }
}
