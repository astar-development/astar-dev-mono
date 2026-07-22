using Microsoft.Extensions.Configuration;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

public class GivenTheExternalLoggingSettingsFile
{
    private static IConfigurationRoot CreateSut() =>
        new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile(Configuration.ExternalSettingsFile).Build();

    [Fact]
    public void when_the_settings_file_is_loaded_then_a_file_sink_entry_is_present_in_write_to()
    {
        var writeTo = CreateSut().GetSection("Serilog:WriteTo").GetChildren().ToArray();

        writeTo.ShouldContain(entry => entry["Name"] == "File");
    }

    [Fact]
    public void when_the_file_sink_entry_is_read_then_it_rolls_daily()
    {
        var fileSink = CreateSut().GetSection("Serilog:WriteTo").GetChildren().Single(entry => entry["Name"] == "File");

        fileSink["Args:rollingInterval"].ShouldBe("Day");
    }

    [Fact]
    public void when_the_file_sink_entry_is_read_then_it_retains_thirty_one_files()
    {
        var fileSink = CreateSut().GetSection("Serilog:WriteTo").GetChildren().Single(entry => entry["Name"] == "File");

        fileSink["Args:retainedFileCountLimit"].ShouldBe("31");
    }

    [Fact]
    public void when_the_file_sink_entry_is_read_then_the_path_uses_the_daily_rolling_file_name_pattern()
    {
        var fileSink = CreateSut().GetSection("Serilog:WriteTo").GetChildren().Single(entry => entry["Name"] == "File");

        fileSink["Args:path"].ShouldBe("logs/log-.txt");
    }

    [Fact]
    public void when_the_settings_file_is_loaded_then_the_existing_seq_and_application_insights_sinks_are_still_present()
    {
        var writeTo = CreateSut().GetSection("Serilog:WriteTo").GetChildren().ToArray();

        writeTo.ShouldContain(entry => entry["Name"] == "Seq");
        writeTo.ShouldContain(entry => entry["Name"] == "ApplicationInsights");
    }
}
