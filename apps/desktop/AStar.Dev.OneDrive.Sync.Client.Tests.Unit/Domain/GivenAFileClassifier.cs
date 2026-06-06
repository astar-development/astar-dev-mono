using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFileClassifier
{
    private static KeywordMapping Mapping(string keyword, string level1, string? level2 = null, string? level3 = null, bool isSpecial = false)
        => ((Result<KeywordMapping, string>.Ok)KeywordMappingFactory.Create(
            keyword,
            level1,
            level2 is not null ? Option.Some(level2) : Option.None<string>(),
            level3 is not null ? Option.Some(level3) : Option.None<string>(),
            isSpecial)).Value;

    [Fact]
    public void when_classifying_segment_based_path_then_path_segments_produce_tokens()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("tuscany", "Travel")];

        var result = FileClassifier.Classify("/Photos/Tuscany/IMG_001.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Travel");
    }

    [Fact]
    public void when_classifying_compound_filename_then_hyphen_separated_parts_produce_tokens()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("car", "Vehicle")];

        var result = FileClassifier.Classify("/docs/red-car-landscape.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Vehicle");
    }

    [Fact]
    public void when_classifying_path_with_mixed_separators_then_all_delimiter_types_are_split()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("finance", "Category")];

        var result = FileClassifier.Classify("/My_Documents/Finance Reports/Q1-2026.xlsx", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Category");
    }

    [Fact]
    public void when_classifying_root_only_path_then_filename_parts_produce_tokens()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("somefile", "Misc")];

        var result = FileClassifier.Classify("/somefile.txt", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Misc");
    }

    [Fact]
    public void when_one_mapping_keyword_appears_in_tokens_then_result_contains_that_classification()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("landscape", "Subject", "Landscape")];

        var result = FileClassifier.Classify("/photos/landscape.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].TagName.ShouldBe("Landscape");
    }

    [Fact]
    public void when_multiple_mapping_keywords_appear_in_tokens_then_result_contains_all_classifications()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("red", "Colour", "Red"), Mapping("car", "Subject", "Vehicle", "Car")];

        var result = FileClassifier.Classify("/photos/red-car.jpg", mappings);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void when_no_mapping_keywords_appear_in_tokens_then_empty_list_is_returned()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("spacecraft", "Science")];

        var result = FileClassifier.Classify("/docs/report.pdf", mappings);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_mappings_list_is_empty_then_empty_list_is_returned()
    {
        var result = FileClassifier.Classify("/photos/beach.jpg", (IReadOnlyList<KeywordMapping>)[]);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_one_of_several_mappings_matches_then_only_that_classification_is_returned()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("car", "Transport"), Mapping("vehicle", "Transport"), Mapping("auto", "Transport")];

        var result = FileClassifier.Classify("/photos/car-show.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Transport");
    }

    [Fact]
    public void when_keyword_matches_case_insensitively_then_mapping_fires()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("photos", "Category")];

        var result = FileClassifier.Classify("/PHOTOS/beach.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("Category");
    }

    [Fact]
    public void when_classifying_with_keyword_mapping_and_no_match_then_empty_list_is_returned()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("spacecraft", "Science")];

        var result = FileClassifier.Classify("/docs/report.pdf", mappings);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_classifying_with_multiple_keyword_mappings_and_both_match_then_both_classifications_are_returned()
    {
        IReadOnlyList<KeywordMapping> mappings = [Mapping("red", "Colour", "Red"), Mapping("car", "Subject", "Vehicle")];

        var result = FileClassifier.Classify("/photos/red-car.jpg", mappings);

        result.Count.ShouldBe(2);
    }
}
