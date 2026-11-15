using System.Text;
using AStarDev.CloudSync.Notifications;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenAWindowsNotificationCommandBuilder
{
    private const string Program = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";

    private static string Decoded(NotificationCommand command) => Encoding.Unicode.GetString(Convert.FromBase64String(command.Arguments[^1]));

    [Fact]
    public void when_a_notification_is_built_then_the_title_and_body_travel_in_the_environment()
    {
        var command = WindowsNotificationCommandBuilder.Build(Program, DesktopNotificationFactory.Create("AStar Dev CloudSync", "Photos: 3 files synced."));

        command.Environment.ShouldBe(new Dictionary<string, string> { [WindowsNotificationCommandBuilder.TitleVariable] = "AStar Dev CloudSync", [WindowsNotificationCommandBuilder.BodyVariable] = "Photos: 3 files synced." });
    }

    [Theory]
    [InlineData("$(Remove-Item -Recurse C:\\)")]
    [InlineData("'; Start-Process calc; '")]
    [InlineData("<toast><visual/></toast>")]
    [InlineData("`whoami` & del *.*")]
    public void when_the_text_looks_like_script_or_markup_then_it_is_not_in_the_arguments_or_the_script(string body)
    {
        var command = WindowsNotificationCommandBuilder.Build(Program, DesktopNotificationFactory.Create("Title", body));

        (command.Arguments.Any(argument => argument.Contains(body, StringComparison.Ordinal)), Decoded(command).Contains(body, StringComparison.Ordinal), command.Environment[WindowsNotificationCommandBuilder.BodyVariable]).ShouldBe((false, false, body));
    }

    [Fact]
    public void when_a_notification_is_built_then_powershell_runs_a_fixed_encoded_script_without_a_profile()
    {
        var command = WindowsNotificationCommandBuilder.Build(Program, DesktopNotificationFactory.Create("Title", "Body"));

        (command.Program, string.Join(" ", command.Arguments.Take(3)), Decoded(command)).ShouldBe((Program, "-NoProfile -NonInteractive -EncodedCommand", WindowsNotificationCommandBuilder.Script));
    }

    [Fact]
    public void when_the_script_is_read_then_it_takes_the_text_from_the_environment_as_text_nodes()
    {
        var script = WindowsNotificationCommandBuilder.Script;

        (script.Contains($"$env:{WindowsNotificationCommandBuilder.TitleVariable}", StringComparison.Ordinal), script.Contains($"$env:{WindowsNotificationCommandBuilder.BodyVariable}", StringComparison.Ordinal), script.Contains("CreateTextNode", StringComparison.Ordinal)).ShouldBe((true, true, true));
    }

    [Fact]
    public void when_the_text_is_missing_then_the_environment_values_are_empty()
    {
        var command = WindowsNotificationCommandBuilder.Build(Program, DesktopNotificationFactory.Create(null, null));

        command.Environment.Values.ShouldBe([string.Empty, string.Empty]);
    }

    [Fact]
    public void when_powershell_is_under_the_system_folder_then_that_fixed_path_is_found()
    {
        var found = WindowsNotificationCommandBuilder.FindProgram(@"C:\Windows\System32", path => path == Program);

        found.ShouldBe(Option.Some(Program));
    }

    [Fact]
    public void when_powershell_is_not_under_the_system_folder_then_nothing_is_found_so_path_is_never_searched()
    {
        WindowsNotificationCommandBuilder.FindProgram(@"C:\Windows\System32", _ => false).ShouldBe(Option.None<string>());
    }
}
