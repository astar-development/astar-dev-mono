using AStarDev.Web.Components.Common;
using Bunit;

namespace AStarDev.Web.TestsUnit.Components.Common;

public class GivenACopyButton : Bunit.BunitContext
{
    [Fact]
    public void when_clipboard_is_supported_then_the_copy_button_is_shown()
    {
        JSInterop.Setup<bool>("astarClipboard.isSupported").SetResult(true);

        var cut = Render<CopyButton>(parameters => parameters.Add(p => p.Text, "dotnet add package AStarDev.Utilities"));

        cut.WaitForAssertion(() => cut.Find("button.copy-btn").ShouldNotBeNull());
    }

    [Fact]
    public void when_clipboard_is_not_supported_then_no_button_is_shown()
    {
        JSInterop.Setup<bool>("astarClipboard.isSupported").SetResult(false);

        var cut = Render<CopyButton>(parameters => parameters.Add(p => p.Text, "dotnet add package AStarDev.Utilities"));

        cut.WaitForAssertion(() => cut.FindAll("button.copy-btn").ShouldBeEmpty());
    }

    [Fact]
    public void when_the_button_is_clicked_then_the_aria_label_changes_to_copied()
    {
        JSInterop.Setup<bool>("astarClipboard.isSupported").SetResult(true);
        JSInterop.Setup<bool>("astarClipboard.copy", _ => true).SetResult(true);
        var cut = Render<CopyButton>(parameters => parameters.Add(p => p.Text, "dotnet add package AStarDev.Utilities"));
        cut.WaitForAssertion(() => cut.Find("button.copy-btn").ShouldNotBeNull());

        cut.Find("button.copy-btn").Click();

        cut.WaitForAssertion(() => cut.Find("button.copy-btn").GetAttribute("aria-label").ShouldBe("Copied!"));
    }
}
