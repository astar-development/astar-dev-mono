using System.Text.Json;
using System.Text.Json.Serialization;

namespace AStarDev.Utilities.TestsUnit;

public sealed class GivenConstants
{
    [Fact]
    public void when_web_deserialisation_settings_are_requested_then_property_names_are_case_insensitive() =>
        Constants.WebDeserialisationSettings.PropertyNameCaseInsensitive.ShouldBeTrue();

    [Fact]
    public void when_web_deserialisation_settings_are_requested_then_property_naming_policy_is_camel_case() =>
        Constants.WebDeserialisationSettings.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);

    [Fact]
    public void when_web_deserialisation_settings_are_requested_then_number_handling_allows_reading_from_string() =>
        Constants.WebDeserialisationSettings.NumberHandling.ShouldBe(JsonNumberHandling.AllowReadingFromString);

    [Fact]
    public void when_web_deserialisation_settings_are_requested_then_write_indented_is_false() =>
        Constants.WebDeserialisationSettings.WriteIndented.ShouldBeFalse();
}
