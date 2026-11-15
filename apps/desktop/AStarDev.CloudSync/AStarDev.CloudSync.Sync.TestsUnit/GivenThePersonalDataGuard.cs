using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenThePersonalDataGuard
{
    public static TheoryData<string> Values => [.. PersonalData.All];

    [Theory]
    [MemberData(nameof(Values))]
    public void when_a_personal_value_is_logged_then_the_guard_sees_it(string value)
    {
        using var logs = new RecordingLoggerProvider();
        var message = $"Handled {value} today";
        logs.Logger.Log(LogLevel.Information, new EventId(1), message, null, (state, _) => state);

        LoggedText.Personal(LoggedText.From(logs), PersonalData.All).ShouldNotBeEmpty();
    }
}
