using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAClassificationCombiner
{
    private static readonly FileClassification AnyRuleClassification = FileClassificationFactory.Create("Memories", Option.Some("Holidays"), Option.None<string>(), false);
    private static readonly FileClassification AnyAnalyserClassification = FileClassificationFactory.Create("Nature", Option.Some("Wildlife"), Option.None<string>(), false);
    private static readonly FileClassification AnySharedClassification = FileClassificationFactory.Create("Memories", Option.Some("Holidays"), Option.None<string>(), false);

    [Fact]
    public void when_both_inputs_empty_then_result_contains_only_unclassified()
    {
        var result = ClassificationCombiner.Combine([], []);

        result.ShouldHaveSingleItem();
        result[0].ShouldBe(FileClassificationFactory.CreateUnclassified());
    }

    [Fact]
    public void when_rule_results_only_then_result_equals_rule_results()
    {
        IReadOnlyList<FileClassification> ruleResults = [AnyRuleClassification];

        var result = ClassificationCombiner.Combine(ruleResults, []);

        result.ShouldBe([AnyRuleClassification]);
    }

    [Fact]
    public void when_analyser_results_only_then_result_equals_analyser_results()
    {
        IReadOnlyList<FileClassification> analyserResults = [AnyAnalyserClassification];

        var result = ClassificationCombiner.Combine([], analyserResults);

        result.ShouldBe([AnyAnalyserClassification]);
    }

    [Fact]
    public void when_both_have_distinct_classifications_then_result_is_union()
    {
        IReadOnlyList<FileClassification> ruleResults = [AnyRuleClassification];
        IReadOnlyList<FileClassification> analyserResults = [AnyAnalyserClassification];

        var result = ClassificationCombiner.Combine(ruleResults, analyserResults);

        result.Count.ShouldBe(2);
        result.ShouldContain(AnyRuleClassification);
        result.ShouldContain(AnyAnalyserClassification);
    }

    [Fact]
    public void when_rule_and_analyser_have_same_L1_L2_L3_then_deduped_to_single_entry()
    {
        var ruleVersion = FileClassificationFactory.Create("Memories", Option.Some("Holidays"), Option.None<string>(), false);
        var analyserVersion = FileClassificationFactory.Create("Memories", Option.Some("Holidays"), Option.None<string>(), true);
        IReadOnlyList<FileClassification> ruleResults = [ruleVersion];
        IReadOnlyList<FileClassification> analyserResults = [analyserVersion];

        var result = ClassificationCombiner.Combine(ruleResults, analyserResults);

        result.ShouldHaveSingleItem();
        result[0].ShouldBe(ruleVersion);
    }

    [Fact]
    public void when_rule_result_is_empty_and_analyser_result_is_empty_then_returns_unclassified()
    {
        var result = ClassificationCombiner.Combine([], []);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Unclassified");
    }
}
