using AStarDev.CloudSync.Logging;
using Avalonia.Input;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenTheLogViewerHotkey
{
    private static KeyEventArgs Press(Key key, KeyModifiers modifiers) => new() { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers };

    [Fact]
    public void when_control_shift_alt_and_l_are_pressed_then_it_is_the_hotkey()
    {
        LogViewerHotkey.Matches(Press(Key.L, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(Key.L, KeyModifiers.Control)]
    [InlineData(Key.L, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData(Key.L, KeyModifiers.Shift | KeyModifiers.Alt)]
    [InlineData(Key.K, KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt)]
    public void when_a_different_combination_is_pressed_then_it_is_not_the_hotkey(Key key, KeyModifiers modifiers)
    {
        LogViewerHotkey.Matches(Press(key, modifiers)).ShouldBeFalse();
    }
}
