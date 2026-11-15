using AStarDev.CloudSync.Autostart;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenARunCommandBuilder
{
    [Fact]
    public void when_a_path_is_given_then_it_is_quoted_and_followed_by_the_minimised_flag()
    {
        RunCommandBuilder.Build(@"C:\Program Files\CloudSync\CloudSync.exe").ShouldBe("\"C:\\Program Files\\CloudSync\\CloudSync.exe\" --minimized");
    }

    [Fact]
    public void when_the_path_holds_a_quote_then_the_quote_is_dropped_so_no_argument_can_be_added()
    {
        RunCommandBuilder.Build("C:\\x\" --evil \"y.exe").ShouldBe("\"C:\\x --evil y.exe\" --minimized");
    }

    [Fact]
    public void when_the_path_holds_a_line_break_then_it_is_dropped()
    {
        RunCommandBuilder.Build("C:\\a\r\nb.exe").ShouldBe("\"C:\\ab.exe\" --minimized");
    }
}
