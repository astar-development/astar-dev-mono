using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFileClassifier
{
    private static FileClassificationRule Rule(string keyword, string level1, string? level2 = null, string? level3 = null, bool isSpecial = false)
        => FileClassificationRuleFactory.Create(
            [keyword],
            FileClassificationFactory.Create(
                level1,
                level2 is not null ? Option.Some(level2) : Option.None<string>(),
                level3 is not null ? Option.Some(level3) : Option.None<string>(),
                isSpecial));

    [Fact]
    public void when_classifying_segment_based_path_then_path_segments_produce_tokens()
    {
        var rule = Rule("tuscany", "Travel");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/Photos/Tuscany/IMG_001.jpg", rules);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Travel");
    }

    [Fact]
    public void when_classifying_compound_filename_then_hyphen_separated_parts_produce_tokens()
    {
        var rule = Rule("car", "Vehicle");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/docs/red-car-landscape.jpg", rules);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Vehicle");
    }

    [Fact]
    public void when_classifying_path_with_mixed_separators_then_all_delimiter_types_are_split()
    {
        var rule = Rule("finance", "Category");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/My_Documents/Finance Reports/Q1-2026.xlsx", rules);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Category");
    }

    [Fact]
    public void when_classifying_root_only_path_then_filename_parts_produce_tokens()
    {
        var rule = Rule("somefile", "Misc");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/somefile.txt", rules);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Misc");
    }

    [Fact]
    public void when_one_rule_keyword_appears_in_tokens_then_result_contains_that_classification()
    {
        var rule = Rule("landscape", "Subject", "Landscape");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/photos/landscape.jpg", rules);

        result.ShouldHaveSingleItem();
        result[0].TagName.ShouldBe("Landscape");
    }

    [Fact]
    public void when_multiple_rule_keywords_appear_in_tokens_then_result_contains_all_classifications()
    {
        var redRule = Rule("red", "Colour", "Red");
        var carRule = Rule("car", "Subject", "Vehicle", "Car");
        var rules = new[] { redRule, carRule };

        var result = FileClassifier.Classify("/photos/red-car.jpg", rules);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void when_no_rule_keywords_appear_in_tokens_then_result_contains_unclassified_sentinel()
    {
        var rule = Rule("spacecraft", "Science");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/docs/report.pdf", rules);

        result.ShouldHaveSingleItem();
        result[0].TagName.ShouldBe("Unclassified");
    }

    [Fact]
    public void when_rule_has_multiple_keywords_and_one_matches_then_rule_contributes_its_classification()
    {
        var rule = FileClassificationRuleFactory.Create(
            ["car", "vehicle", "auto"],
            FileClassificationFactory.Create("Transport", Option.None<string>(), Option.None<string>(), false));
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/photos/car-show.jpg", rules);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Transport");
    }

    [Fact]
    public void when_rules_list_is_empty_then_result_contains_unclassified_sentinel()
    {
        var result = FileClassifier.Classify("/photos/beach.jpg", []);

        result.ShouldHaveSingleItem();
        result[0].TagName.ShouldBe("Unclassified");
    }

    [Fact]
    public void when_keyword_matches_case_insensitively_then_rule_fires()
    {
        var rule = Rule("photos", "Category");
        var rules = new[] { rule };

        var result = FileClassifier.Classify("/PHOTOS/beach.jpg", rules);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Category");
    }
}
