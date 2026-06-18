using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAFileClassifier
{
    private static FileClassificationCategory Category(string name, int level, bool isFamous = false, bool isInternet = false)
        => ((Result<FileClassificationCategory, string>.Ok)FileClassificationCategoryFactory.Create(new(1), name, level, isFamous, isInternet, Option.None<FileClassificationCategoryId>())).Value;

    [Fact]
    public void when_classifying_segment_based_path_then_path_segments_produce_tokens()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("tuscany", 1)];

        var result = FileClassifier.Classify("/Photos/Tuscany/IMG_001.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("tuscany");
    }

    [Fact]
    public void when_classifying_compound_filename_then_hyphen_separated_parts_produce_tokens()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("car", 1)];

        var result = FileClassifier.Classify("/docs/red-car-landscape.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("car");
    }

    [Fact]
    public void when_classifying_path_with_mixed_separators_then_all_delimiter_types_are_split()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("finance", 1)];

        var result = FileClassifier.Classify("/My_Documents/Finance Reports/Q1-2026.xlsx", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("finance");
    }

    [Fact]
    public void when_classifying_root_only_path_then_filename_parts_produce_tokens()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("somefile", 1)];

        var result = FileClassifier.Classify("/somefile.txt", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("somefile");
    }

    [Fact]
    public void when_one_mapping_keyword_appears_in_tokens_then_result_contains_that_classification()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("landscape", 1)];

        var result = FileClassifier.Classify("/photos/landscape.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("landscape");
    }

    [Fact]
    public void when_multiple_mapping_keywords_appear_in_tokens_then_result_contains_all_classifications()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("red", 1), Category("car", 1)];

        var result = FileClassifier.Classify("/photos/red-car.jpg", mappings);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void when_no_mapping_keywords_appear_in_tokens_then_empty_list_is_returned()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("spacecraft", 1)];

        var result = FileClassifier.Classify("/docs/report.pdf", mappings);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_mappings_list_is_empty_then_empty_list_is_returned()
    {
        var result = FileClassifier.Classify("/photos/beach.jpg", (IReadOnlyList<FileClassificationCategory>)[]);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_one_of_several_mappings_matches_then_only_that_classification_is_returned()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("car", 1), Category("vehicle", 1), Category("auto", 1)];

        var result = FileClassifier.Classify("/photos/car-show.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("car");
    }

    [Fact]
    public void when_keyword_matches_case_insensitively_then_mapping_fires()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("photos", 1)];

        var result = FileClassifier.Classify("/PHOTOS/beach.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("photos");
    }

    [Fact]
    public void when_classifying_with_keyword_mapping_and_no_match_then_empty_list_is_returned()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("spacecraft", 1)];

        var result = FileClassifier.Classify("/docs/report.pdf", mappings);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_classifying_with_multiple_keyword_mappings_and_both_match_then_both_classifications_are_returned()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("red", 1), Category("car", 1)];

        var result = FileClassifier.Classify("/photos/red-car.jpg", mappings);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void when_keyword_has_spaces_and_path_contains_spaceless_compound_then_mapping_fires()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("red car", 1)];

        var result = FileClassifier.Classify("/photos/redcar.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("red car");
    }

    [Fact]
    public void when_keyword_has_spaces_and_path_contains_spaceless_compound_in_directory_then_mapping_fires()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("red car", 1)];

        var result = FileClassifier.Classify("/photos/redcar/IMG001.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("red car");
    }

    [Fact]
    public void when_keyword_has_multiple_spaces_and_path_contains_fully_compounded_form_then_mapping_fires()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("big red car", 1)];

        var result = FileClassifier.Classify("/photos/bigredcar.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("big red car");
    }

    [Fact]
    public void when_keyword_has_spaces_and_all_words_appear_as_tokens_then_mapping_fires()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("red car", 1)];

        var result = FileClassifier.Classify("/photos/red car/IMG001.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("red car");
    }

    [Fact]
    public void when_classifying_path_with_plus_separator_then_plus_delimited_parts_produce_tokens()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("tuscany", 1)];

        var result = FileClassifier.Classify("/photos/italy+tuscany+2024.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("tuscany");
    }

    [Fact]
    public void when_keyword_matches_token_from_plus_separated_filename_then_mapping_fires()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("beach", 1)];

        var result = FileClassifier.Classify("/photos/summer+beach+sunset.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("beach");
    }

    [Fact]
    public void when_path_has_no_plus_then_behaviour_unchanged_after_separator_addition()
    {
        IReadOnlyList<FileClassificationCategory> mappings = [Category("car", 1)];

        var result = FileClassifier.Classify("/docs/red-car-landscape.jpg", mappings);

        result.ShouldHaveSingleItem();
        result[0].Level1.ShouldBe("car");
    }
}
