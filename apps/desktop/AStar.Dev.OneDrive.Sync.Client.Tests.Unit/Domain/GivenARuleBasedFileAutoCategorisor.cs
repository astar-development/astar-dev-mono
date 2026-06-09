using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenARuleBasedFileAutoCategorisor
{
    private readonly IFileAutoCategorisor sut = new RuleBasedFileAutoCategorisor();

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level1_is_color()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Color");
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level1_is_person()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level2_is_red()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level2_is_john_smith()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("John Smith");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level3_is_red_car()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_dress_path_then_level3_is_red_dress()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red dress on the floor.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Dress");
    }

    [Fact]
    public void when_categorise_called_with_misc_red_path_then_level3_is_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level3_is_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_full_classification_is_correct()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Color");
        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
        classification.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_dress_path_then_full_classification_is_correct()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red dress on the floor.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Color");
        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
        classification.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Dress");
    }

    [Fact]
    public void when_categorise_called_with_misc_red_path_then_full_classification_is_correct()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Color");
        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
        classification.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_full_classification_is_correct()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Person");
        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("John Smith");
        classification.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_empty_path_then_does_not_throw()
    {
        Should.NotThrow(() => sut.Categorise(string.Empty));
    }

    [Fact]
    public void when_categorise_called_with_empty_path_then_returns_none()
    {
        Option<FileClassification> result = sut.Categorise(string.Empty);

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_only_root_segments_then_does_not_throw()
    {
        Should.NotThrow(() => sut.Categorise("a/b/c/d/e/f/g"));
    }

    [Fact]
    public void when_categorise_called_with_only_root_segments_then_returns_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_does_not_throw()
    {
        Should.NotThrow(() => sut.Categorise("a/b/c/d/e/f/g/a/the.jpg"));
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_level2_is_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_level3_is_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_is_special_is_false()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.IsSpecial.ShouldBeFalse();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_is_special_is_false()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.IsSpecial.ShouldBeFalse();
    }

    [Fact]
    public void when_categorise_called_with_empty_path_then_is_special_is_false()
    {
        Option<FileClassification> result = sut.Categorise(string.Empty);

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_places_folder_path_then_level1_is_place()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Places/a scenic lake view.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Place");
    }

    [Fact]
    public void when_categorise_called_with_landscapes_folder_path_then_level1_is_place()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Landscapes/a scenic lake view.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Place");
    }

    [Fact]
    public void when_categorise_called_with_events_folder_path_then_level1_is_event()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Events/birthday party.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Event");
    }

    [Fact]
    public void when_categorise_called_with_portraits_folder_path_then_level1_is_person()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Portraits/a file with a persons name: jane doe.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_people_folder_and_colour_in_filename_then_level1_is_person()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/john smith blue shirt.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_people_folder_and_colour_in_filename_then_level2_is_person_name_not_colour()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/john smith blue shirt.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("John Smith");
    }

    [Fact]
    public void when_categorise_called_with_people_folder_and_colour_in_filename_then_level3_is_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/People/john smith blue shirt.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_unknown_folder_and_person_name_in_filename_then_level1_is_person()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Uncategorised/jane doe portrait.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_photos_blue_car_path_then_level1_is_color()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a blue car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level1.ShouldBe("Color");
    }

    [Fact]
    public void when_categorise_called_with_photos_blue_car_path_then_level2_is_blue()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a blue car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Blue");
    }

    [Fact]
    public void when_categorise_called_with_photos_blue_car_path_then_level3_is_blue_car()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a blue car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBe("Blue Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_green_hat_path_then_level2_is_green()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a green hat on the table.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Green");
    }

    [Fact]
    public void when_categorise_called_with_photos_green_hat_path_then_level3_is_green_hat()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a green hat on the table.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBe("Green Hat");
    }

    [Fact]
    public void when_categorise_called_with_colour_only_filename_then_level2_is_colour()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Misc/blue.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level2.Match(v => (string?)v, () => null).ShouldBe("Blue");
    }

    [Fact]
    public void when_categorise_called_with_colour_only_filename_then_level3_is_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Misc/blue.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_tag_name_requested_and_level3_is_present_then_tag_name_is_level3()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.TagName.ShouldBe("Red Car");
    }

    [Fact]
    public void when_tag_name_requested_and_level3_is_absent_and_level2_is_present_then_tag_name_is_level2()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");
        FileClassification classification = result.Match(c => c, () => throw new InvalidOperationException("Expected Some"));

        classification.TagName.ShouldBe("Red");
    }

    [Fact]
    public void when_tag_name_requested_and_level2_and_level3_are_absent_then_returns_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_returns_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_generic_path_and_no_keyword_match_then_returns_none()
    {
        Option<FileClassification> result = sut.Categorise("path/to/generic/file.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_categorise_called_with_photos_folder_and_no_colour_or_person_in_filename_then_returns_none()
    {
        Option<FileClassification> result = sut.Categorise("a/b/c/d/e/f/g/Photos/img001.jpg");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }
}
