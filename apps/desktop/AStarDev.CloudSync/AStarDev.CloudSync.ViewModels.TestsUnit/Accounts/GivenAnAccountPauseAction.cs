using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountPauseAction
{
    [Theory]
    [InlineData("up-to-date", "Pause")]
    [InlineData("syncing", "Pause")]
    [InlineData("conflicts", "Pause")]
    [InlineData("paused", "Resume")]
    public void when_the_account_is_in_a_state_then_the_action_reads_pause_unless_it_is_paused(string state, string expected)
    {
        var account = Create(state);

        (account.PauseText, account.CanPause).ShouldBe((expected, true));
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_it_cannot_be_paused_or_resumed()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), new RecordingAccountCommands());

        account.CanPause.ShouldBeFalse();
    }

    private static AccountViewModel Create(string state) => new(TestData.CreateAccount(state switch
    {
        "syncing" => SyncStatusFactory.CreateSyncing(25),
        "conflicts" => SyncStatusFactory.CreateHasConflicts(2),
        "paused" => SyncStatusFactory.CreatePaused(),
        _ => SyncStatusFactory.CreateUpToDate()
    }, Option.None<DateTimeOffset>()), [], NoAccountCommands.Instance, TestData.CreateLocalization(), TestData.CreateTime());
}
