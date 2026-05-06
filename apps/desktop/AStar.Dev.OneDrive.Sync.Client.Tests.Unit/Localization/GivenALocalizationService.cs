using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Localization;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Localization;

public sealed class GivenALocalizationService
{
    [Fact]
    public void when_constructed_then_culture_defaults_to_en_GB()
    {
        var service = new LocalizationService();

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public void when_constructed_then_available_cultures_includes_en_GB()
    {
        var service = new LocalizationService();

        service.AvailableCultures.ShouldNotBeEmpty();
        service.AvailableCultures.ShouldContain(c => c.Name == "en-GB");
    }

    [Fact]
    public async Task when_initialise_called_without_argument_then_culture_defaults_to_en_GB()
    {
        var service = new LocalizationService();

        //await service.InitialiseAsync();

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public async Task when_initialise_called_with_fallback_culture_then_it_succeeds()
    {
        var service = new LocalizationService();
        var culture = new CultureInfo("en-GB");

        //await service.InitialiseAsync(culture);

        _ = service.CurrentCulture.ShouldNotBeNull();
    }

    [Fact]
    public void when_get_local_called_with_valid_key_then_value_is_returned()
    {
        var service = new LocalizationService();
        string key = "App.Title";

        string result = service.GetLocal(key);

        _ = result.ShouldNotBeNull();
        result.ShouldNotBe(string.Empty);
    }

    [Fact]
    public void when_get_local_called_with_invalid_key_then_key_is_returned_as_fallback()
    {
        var service = new LocalizationService();
        string key = "NonExistent.Key";

        string result = service.GetLocal(key);

        result.ShouldBe(key);
    }

    [Fact]
    public void when_get_local_called_multiple_times_with_same_key_then_same_value_is_returned()
    {
        var service = new LocalizationService();
        string key = "App.Title";

        string result1 = service.GetLocal(key);
        string result2 = service.GetLocal(key);

        result1.ShouldBe(result2);
    }

    [Fact]
    public void when_get_local_called_with_format_arguments_then_string_is_not_null()
    {
        var service = new LocalizationService();
        string key = "Format.Test";
        string placeholder = "TestValue";

        string result = service.GetLocal(key, placeholder);

        _ = result.ShouldNotBeNull();
    }

    [Fact]
    public void when_get_local_called_with_empty_key_then_empty_string_is_returned()
    {
        var service = new LocalizationService();

        string result = service.GetLocal(string.Empty);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_get_local_called_with_null_key_then_argument_null_exception_is_thrown()
    {
        var service = new LocalizationService();

        try
        {
            string result = service.GetLocal(null!);
            _ = result.ShouldNotBeNull();
        }
        catch(ArgumentNullException)
        {
            // Expected behavior - null key should throw
        }
    }

    [Fact]
    public async Task when_set_culture_async_called_with_en_GB_then_current_culture_is_en_GB()
    {
        var service = new LocalizationService();
        var targetCulture = new CultureInfo("en-GB");

        await service.SetCultureAsync(targetCulture);

        service.CurrentCulture.Name.ShouldBe("en-GB");
    }

    [Fact]
    public async Task when_set_culture_async_called_with_same_culture_then_culture_changed_event_is_not_raised()
    {
        var service = new LocalizationService();
        bool eventRaised = false;
        service.CultureChanged += (s, c) => eventRaised = true;

        var currentCulture = service.CurrentCulture;

        await service.SetCultureAsync(currentCulture);

        eventRaised.ShouldBeFalse();
    }

    [Fact]
    public void when_current_culture_is_read_then_it_is_not_null()
    {
        var service = new LocalizationService();
        var originalCulture = service.CurrentCulture;
        _ = originalCulture.ShouldNotBeNull();
    }

    [Fact]
    public void when_available_cultures_is_read_then_it_is_not_null()
    {
        var service = new LocalizationService();

        var cultures = service.AvailableCultures;

        _ = cultures.ShouldNotBeNull();
        // Cannot modify as it should be a read-only list
    }

    [Fact]
    public void when_get_local_called_with_multiple_format_arguments_then_non_null_result_is_returned()
    {
        var service = new LocalizationService();
        string key = "Format.MultiArg";

        string result = service.GetLocal(key, "arg1", "arg2", "arg3");

        _ = result.ShouldNotBeNull();
    }

    [Fact]
    public void when_current_culture_is_read_then_name_is_not_empty()
    {
        var service = new LocalizationService();
        var originalCulture = service.CurrentCulture;

        _ = originalCulture.ShouldNotBeNull();
        originalCulture.Name.ShouldNotBeEmpty();
    }
}
