using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.WallpaperScraper.ScrapeConfiguration;
using Testably.Abstractions.Testing;

namespace AStarDev.WallpaperScraper.TestsUnit.ScrapeConfiguration;

public sealed class GivenADirectorySettingsInput
{
    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_value()
    {
        var result = new DirectorySettingsInput("/pictures", "/pictures/famous", "wallhaven").Validate();

        var settings = result.ShouldBeOfType<Valid<DirectorySettings>>().Value;
        settings.RootDirectory.ShouldBe("/pictures");
        settings.RootDirectoryFamous.ShouldBe("/pictures/famous");
        settings.SubDirectoryName.ShouldBe("wallhaven");
    }

    [Fact]
    public void when_the_directories_have_surrounding_whitespace_then_they_are_trimmed()
    {
        var result = new DirectorySettingsInput("  /pictures  ", " /pictures/famous ", " wallhaven ").Validate();

        var settings = result.ShouldBeOfType<Valid<DirectorySettings>>().Value;
        settings.RootDirectory.ShouldBe("/pictures");
        settings.RootDirectoryFamous.ShouldBe("/pictures/famous");
        settings.SubDirectoryName.ShouldBe("wallhaven");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_root_directory_is_blank_then_it_is_rejected(string enteredText) =>
        ErrorsFor(new DirectorySettingsInput(enteredText, "/famous", "sub")).ShouldContain(error => error.Property == nameof(DirectorySettingsInput.RootDirectory));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_famous_root_directory_is_blank_then_it_is_rejected(string enteredText) =>
        ErrorsFor(new DirectorySettingsInput("/root", enteredText, "sub")).ShouldContain(error => error.Property == nameof(DirectorySettingsInput.RootDirectoryFamous));

    [Fact]
    public void when_the_sub_directory_name_is_blank_then_the_input_is_valid() =>
        new DirectorySettingsInput("/root", "/famous", string.Empty).Validate().ShouldBeOfType<Valid<DirectorySettings>>();

    [Fact]
    public void when_both_directories_are_blank_then_both_errors_are_reported() =>
        ErrorsFor(new DirectorySettingsInput(string.Empty, string.Empty, "sub")).Count.ShouldBe(2);

    [Fact]
    public void when_the_directories_exist_then_nothing_is_reported_missing()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/root");
        fileSystem.Directory.CreateDirectory("/famous");

        new DirectorySettingsInput("/root", "/famous", "sub").MissingDirectories(fileSystem).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_directory_does_not_exist_then_it_is_reported_missing()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/root");

        var missing = new DirectorySettingsInput("/root", "/famous", "sub").MissingDirectories(fileSystem);

        missing.Single().ShouldContain("/famous");
        missing.Single().ShouldContain("Famous root directory");
    }

    [Fact]
    public void when_a_directory_is_blank_then_it_is_not_reported_missing() =>
        new DirectorySettingsInput(string.Empty, "   ", "sub").MissingDirectories(new MockFileSystem()).ShouldBeEmpty();

    [Fact]
    public void when_a_path_has_surrounding_whitespace_then_the_trimmed_directory_is_checked()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/root");
        fileSystem.Directory.CreateDirectory("/famous");

        new DirectorySettingsInput(" /root ", "/famous ", "sub").MissingDirectories(fileSystem).ShouldBeEmpty();
    }

    [Fact]
    public void when_created_from_an_entity_then_the_values_are_copied()
    {
        var input = DirectorySettingsInput.From(GivenAUserSettingsInput.CreateEntity());

        input.ShouldBe(new DirectorySettingsInput("root", "famous", "sub"));
    }

    private static IReadOnlyList<ValidationError> ErrorsFor(DirectorySettingsInput input) =>
        input.Validate().ShouldBeOfType<Invalid<DirectorySettings>>().Errors;
}
