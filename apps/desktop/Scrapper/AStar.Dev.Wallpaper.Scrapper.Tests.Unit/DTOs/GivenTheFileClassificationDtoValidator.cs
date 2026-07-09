using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scrapper.DTOs;

namespace AStar.Dev.Wallpaper.Scrapper.Tests.Unit.DTOs;

public sealed class GivenTheFileClassificationDtoValidator
{
    private static FileClassification BuildDto(string name, int level) => new() { Name = name, Level = level, };

    [Fact]
    public void when_the_name_and_level_are_valid_then_the_result_is_valid() =>
        FileClassificationDtoValidator.Validate(BuildDto("Animals", 3), 0).ShouldBeOfType<Valid<FileClassification>>();

    [Fact]
    public void when_the_name_is_empty_then_the_result_is_invalid() =>
        FileClassificationDtoValidator.Validate(BuildDto("", 3), 0).ShouldBeOfType<Invalid<FileClassification>>();

    [Fact]
    public void when_the_name_is_empty_then_the_error_property_identifies_the_row_and_field() =>
        FileClassificationDtoValidator.Validate(BuildDto("", 3), 2).ShouldBeOfType<Invalid<FileClassification>>()
            .Errors.ShouldHaveSingleItem().Property.ShouldBe("Categories[2].Name");

    [Fact]
    public void when_the_level_is_below_the_minimum_then_the_result_is_invalid() =>
        FileClassificationDtoValidator.Validate(BuildDto("Animals", 0), 0).ShouldBeOfType<Invalid<FileClassification>>();

    [Fact]
    public void when_the_level_is_above_the_maximum_then_the_result_is_invalid() =>
        FileClassificationDtoValidator.Validate(BuildDto("Animals", 4), 0).ShouldBeOfType<Invalid<FileClassification>>();

    [Fact]
    public void when_the_level_is_invalid_then_the_error_property_identifies_the_row_and_field() =>
        FileClassificationDtoValidator.Validate(BuildDto("Animals", 9), 1).ShouldBeOfType<Invalid<FileClassification>>()
            .Errors.ShouldHaveSingleItem().Property.ShouldBe("Categories[1].Level");

    [Fact]
    public void when_both_the_name_and_level_are_invalid_then_both_errors_are_accumulated() =>
        FileClassificationDtoValidator.Validate(BuildDto("", 0), 0).ShouldBeOfType<Invalid<FileClassification>>()
            .Errors.Count.ShouldBe(2);

    [Fact]
    public void when_validating_all_rows_and_all_are_valid_then_the_result_is_valid()
    {
        FileClassification[] dtos = [BuildDto("Animals", 3), BuildDto("Cars", 2),];

        FileClassificationDtoValidator.ValidateAll(dtos).ShouldBeOfType<Valid<IReadOnlyList<FileClassification>>>();
    }

    [Fact]
    public void when_validating_all_rows_and_three_rows_are_invalid_then_all_three_errors_are_reported()
    {
        FileClassification[] dtos = [BuildDto("", 3), BuildDto("Valid", 0), BuildDto("Another Valid", 9),];

        var errors = FileClassificationDtoValidator.ValidateAll(dtos).ShouldBeOfType<Invalid<IReadOnlyList<FileClassification>>>().Errors;

        errors.Count.ShouldBe(3);
    }

    [Fact]
    public void when_validating_all_rows_and_three_rows_are_invalid_then_the_errors_are_reported_in_row_order()
    {
        FileClassification[] dtos = [BuildDto("", 3), BuildDto("Valid", 0), BuildDto("Another Valid", 9),];

        var errors = FileClassificationDtoValidator.ValidateAll(dtos).ShouldBeOfType<Invalid<IReadOnlyList<FileClassification>>>().Errors;

        errors[0].Property.ShouldBe("Categories[0].Name");
        errors[1].Property.ShouldBe("Categories[1].Level");
        errors[2].Property.ShouldBe("Categories[2].Level");
    }
}
