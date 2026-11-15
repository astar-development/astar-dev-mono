namespace AStarDev.CloudSync.TestsUnit.Localisation;

public sealed class GivenTheLocalisationScanners
{
    private static readonly HashSet<string> Prefixes = ["Tray", "Notification"];

    [Fact]
    public void when_a_key_is_passed_to_the_localiser_then_the_scan_finds_it()
    {
        var keys = LocalisationSources.KeyLiteralsIn("""localization.GetLocal("Tray.Open", Title)""", Prefixes);

        keys.ShouldBe(["Tray.Open"]);
    }

    [Fact]
    public void when_a_key_sits_in_a_switch_arm_then_the_scan_finds_it()
    {
        var keys = LocalisationSources.KeyLiteralsIn("""SyncNotification.SignInRequired => "Notification.SignInRequired",""", Prefixes);

        keys.ShouldBe(["Notification.SignInRequired"]);
    }

    [Fact]
    public void when_a_literal_has_another_first_segment_then_the_scan_ignores_it()
    {
        LocalisationSources.KeyLiteralsIn("""var name = "System.Text"; var other = "Foo.Bar";""", Prefixes).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_key_is_built_by_interpolation_then_the_scan_ignores_it()
    {
        LocalisationSources.KeyLiteralsIn("""GetLocal($"Tray.{name}")""", Prefixes).ShouldBeEmpty();
    }

    [Fact]
    public void when_user_text_is_assigned_in_code_then_the_scan_finds_it()
    {
        LocalisationSources.UserFacingLiteralsIn("""public string ToggleText => "Start at login"; var Title = "Sync now";""").ShouldBe(["Start at login", "Sync now"]);
    }

    [Fact]
    public void when_a_log_message_or_a_key_is_assigned_then_the_scan_ignores_it()
    {
        var source = """
            [LoggerMessage(EventId = 5310, Level = LogLevel.Error, Message = "The add-account attempt failed unexpectedly.")]
            public string Name => localization.GetLocal("Provider.OneDrive");
            public const string Label = "dev";
            """;

        LocalisationSources.UserFacingLiteralsIn(source).ShouldBeEmpty();
    }

    [Fact]
    public void when_user_text_is_written_in_axaml_then_the_scan_finds_it()
    {
        LocalisationSources.AxamlLiteralsIn("""<TextBlock Text="Hello there" /><Button Content="OK" ToolTip.Tip="Go" />""").ShouldBe(["Hello there", "OK", "Go"]);
    }

    [Fact]
    public void when_axaml_binds_or_holds_no_letters_then_the_scan_ignores_it()
    {
        LocalisationSources.AxamlLiteralsIn("""<TextBlock Text="{Binding Name}" /><TextBlock Text="" /><TextBlock Text="12" /><Border Content="↔" />""").ShouldBeEmpty();
    }

    [Fact]
    public void when_an_environment_variable_name_is_assigned_then_the_scan_ignores_it()
    {
        LocalisationSources.UserFacingLiteralsIn("""public const string TitleVariable = "CLOUDSYNC_NOTIFY_TITLE";""").ShouldBeEmpty();
    }
}
