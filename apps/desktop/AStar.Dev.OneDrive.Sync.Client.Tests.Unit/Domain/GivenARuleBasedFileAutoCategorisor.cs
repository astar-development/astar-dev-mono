using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenARuleBasedFileAutoCategorisor
{
    private readonly IFileAutoCategorisor sut = new RuleBasedFileAutoCategorisor();

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level1_is_color()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.Level1.ShouldBe("Color");
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level1_is_person()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level2_is_red()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level2_is_john_smith()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBe("John Smith");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level3_is_red_car()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_dress_path_then_level3_is_red_dress()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red dress on the floor.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Dress");
    }

    [Fact]
    public void when_categorise_called_with_misc_red_path_then_level3_is_none()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level3_is_none()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.Level1.ShouldBe("Color");
        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
        result.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_dress_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red dress on the floor.jpg");

        result.Level1.ShouldBe("Color");
        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
        result.Level3.Match(v => (string?)v, () => null).ShouldBe("Red Dress");
    }

    [Fact]
    public void when_categorise_called_with_misc_red_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");

        result.Level1.ShouldBe("Color");
        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Red");
        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.Level1.ShouldBe("Person");
        result.Level2.Match(v => (string?)v, () => null).ShouldBe("John Smith");
        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_empty_path_then_does_not_throw()
    {
        Should.NotThrow(() => sut.Categorise(string.Empty));
    }

    [Fact]
    public void when_categorise_called_with_empty_path_then_returns_valid_classification()
    {
        FileClassification result = sut.Categorise(string.Empty);

        result.ShouldNotBeNull();
        result.Level1.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_categorise_called_with_only_root_segments_then_does_not_throw()
    {
        Should.NotThrow(() => sut.Categorise("a/b/c/d/e/f/g"));
    }

    [Fact]
    public void when_categorise_called_with_only_root_segments_then_returns_valid_classification()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g");

        result.ShouldNotBeNull();
        result.Level1.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_does_not_throw()
    {
        Should.NotThrow(() => sut.Categorise("a/b/c/d/e/f/g/a/the.jpg"));
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_level2_is_none()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_no_meaningful_tokens_then_level3_is_none()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_is_special_is_false()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.IsSpecial.ShouldBeFalse();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_is_special_is_false()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.IsSpecial.ShouldBeFalse();
    }

    [Fact]
    public void when_categorise_called_with_empty_path_then_is_special_is_false()
    {
        FileClassification result = sut.Categorise(string.Empty);

        result.IsSpecial.ShouldBeFalse();
    }

    [Fact]
    public void when_categorise_called_with_places_folder_path_then_level1_is_place()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Places/a scenic lake view.jpg");

        result.Level1.ShouldBe("Place");
    }

    [Fact]
    public void when_categorise_called_with_landscapes_folder_path_then_level1_is_place()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Landscapes/a scenic lake view.jpg");

        result.Level1.ShouldBe("Place");
    }

    [Fact]
    public void when_categorise_called_with_events_folder_path_then_level1_is_event()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Events/birthday party.jpg");

        result.Level1.ShouldBe("Event");
    }

    [Fact]
    public void when_categorise_called_with_portraits_folder_path_then_level1_is_person()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Portraits/a file with a persons name: jane doe.jpg");

        result.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_people_folder_and_colour_in_filename_then_level1_is_person()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/john smith blue shirt.jpg");

        result.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_people_folder_and_colour_in_filename_then_level2_is_person_name_not_colour()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/john smith blue shirt.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBe("John Smith");
    }

    [Fact]
    public void when_categorise_called_with_people_folder_and_colour_in_filename_then_level3_is_none()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/john smith blue shirt.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_unknown_folder_and_person_name_in_filename_then_level1_is_person()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Uncategorised/jane doe portrait.jpg");

        result.Level1.ShouldBe("Person");
    }

    [Fact]
    public void when_categorise_called_with_photos_blue_car_path_then_level1_is_color()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a blue car on the road.jpg");

        result.Level1.ShouldBe("Color");
    }

    [Fact]
    public void when_categorise_called_with_photos_blue_car_path_then_level2_is_blue()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a blue car on the road.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Blue");
    }

    [Fact]
    public void when_categorise_called_with_photos_blue_car_path_then_level3_is_blue_car()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a blue car on the road.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBe("Blue Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_green_hat_path_then_level2_is_green()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a green hat on the table.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Green");
    }

    [Fact]
    public void when_categorise_called_with_photos_green_hat_path_then_level3_is_green_hat()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a green hat on the table.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBe("Green Hat");
    }

    [Fact]
    public void when_categorise_called_with_colour_only_filename_then_level2_is_colour()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Misc/blue.jpg");

        result.Level2.Match(v => (string?)v, () => null).ShouldBe("Blue");
    }

    [Fact]
    public void when_categorise_called_with_colour_only_filename_then_level3_is_none()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Misc/blue.jpg");

        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_tag_name_requested_and_level3_is_present_then_tag_name_is_level3()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.TagName.ShouldBe("Red Car");
    }

    [Fact]
    public void when_tag_name_requested_and_level3_is_absent_and_level2_is_present_then_tag_name_is_level2()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");

        result.TagName.ShouldBe("Red");
    }

    [Fact]
    public void when_tag_name_requested_and_level2_and_level3_are_absent_then_tag_name_is_level1()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/a/the.jpg");

        result.TagName.ShouldBe(result.Level1);
    }
}
