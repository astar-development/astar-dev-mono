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

        result.Level2.Match(v => v, () => null).ShouldBe("Red");
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_level2_is_john_smith()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.Level2.Match(v => v, () => null).ShouldBe("John Smith");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_car_path_then_level3_is_red_car()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red car on the road.jpg");

        result.Level3.Match(v => v, () => null).ShouldBe("Red Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_dress_path_then_level3_is_red_dress()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red dress on the floor.jpg");

        result.Level3.Match(v => v, () => null).ShouldBe("Red Dress");
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
        result.Level2.Match(v => v, () => null).ShouldBe("Red");
        result.Level3.Match(v => v, () => null).ShouldBe("Red Car");
    }

    [Fact]
    public void when_categorise_called_with_photos_red_dress_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Photos/a red dress on the floor.jpg");

        result.Level1.ShouldBe("Color");
        result.Level2.Match(v => v, () => null).ShouldBe("Red");
        result.Level3.Match(v => v, () => null).ShouldBe("Red Dress");
    }

    [Fact]
    public void when_categorise_called_with_misc_red_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/Misc/a file with red in it's name.jpg");

        result.Level1.ShouldBe("Color");
        result.Level2.Match(v => v, () => null).ShouldBe("Red");
        result.Level3.Match(v => (string?)v, () => null).ShouldBeNull();
    }

    [Fact]
    public void when_categorise_called_with_people_person_name_path_then_full_classification_is_correct()
    {
        FileClassification result = sut.Categorise("a/b/c/d/e/f/g/People/a file with a persons name: john smith - in it.jpg");

        result.Level1.ShouldBe("Person");
        result.Level2.Match(v => v, () => null).ShouldBe("John Smith");
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
}
