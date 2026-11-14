using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Common;
using AStarDev.CloudSync.TestHelpers.Ui;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountWithAFailedSync
{
    [Theory]
    [InlineData("root", "The sync folder is unavailable. Check the drive is connected, then sync again.")]
    [InlineData("quota", "Cloud storage is full. Free some space, then sync again.")]
    [InlineData("disk", "This computer is out of disk space. Free some space, then sync again.")]
    [InlineData("other", "Sync failed. The details are in the log.")]
    public void when_the_sync_failed_then_layout_a_shows_its_own_message(string reason, string expected)
    {
        var account = TestData.CreateAccountWithFailedSync(Reason(reason));

        (account.HasFailure, account.LayoutAFailureText).ShouldBe((true, expected));
    }

    [Theory]
    [InlineData("root", "Sync stopped because the sync folder is unavailable. Reconnect it or change the sync folder.")]
    [InlineData("quota", "Sync stopped because cloud storage is full. Free some space to continue.")]
    [InlineData("disk", "Sync stopped because this computer is out of disk space. Free some space to continue.")]
    [InlineData("other", "Sync stopped unexpectedly. The details are in the log.")]
    public void when_the_sync_failed_then_layout_b_shows_its_own_message(string reason, string expected)
    {
        var account = TestData.CreateAccountWithFailedSync(Reason(reason));

        account.LayoutBFailureText.ShouldBe(expected);
    }

    [Theory]
    [InlineData("root", "Check the sync folder")]
    [InlineData("quota", "Free cloud space")]
    [InlineData("disk", "Free disk space")]
    [InlineData("other", "Check the log")]
    public void when_the_sync_failed_then_layout_c_shows_its_own_message(string reason, string expected)
    {
        var account = TestData.CreateAccountWithFailedSync(Reason(reason));

        account.LayoutCFailureText.ShouldBe(expected);
    }

    [Fact]
    public void when_the_account_is_up_to_date_then_there_is_no_failure_and_every_message_is_empty()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccount(SyncStatusFactory.CreateUpToDate(), AStarDev.FunctionalParadigm.Option.None<DateTimeOffset>()), new RecordingAccountCommands());

        (account.HasFailure, account.LayoutAFailureText, account.LayoutBFailureText, account.LayoutCFailureText).ShouldBe((false, string.Empty, string.Empty, string.Empty));
    }

    [Fact]
    public void when_the_account_needs_sign_in_then_there_is_no_failure_message()
    {
        var account = TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), new RecordingAccountCommands());

        (account.HasFailure, account.LayoutAFailureText).ShouldBe((false, string.Empty));
    }

    [Fact]
    public void when_the_failure_messages_are_read_then_they_carry_no_account_details()
    {
        var account = TestData.CreateAccountWithFailedSync(SyncFailureReasonFactory.CreateUnexpected());

        string[] messages = [account.LayoutAFailureText, account.LayoutBFailureText, account.LayoutCFailureText];

        messages.ShouldAllBe(message => !message.Contains("work@example.com") && !message.Contains("Work"));
    }

    private static SyncFailureReason Reason(string name) => name switch
    {
        "root" => SyncFailureReasonFactory.CreateRootUnavailable(),
        "quota" => SyncFailureReasonFactory.CreateQuotaExceeded(),
        "disk" => SyncFailureReasonFactory.CreateInsufficientDiskSpace(),
        _ => SyncFailureReasonFactory.CreateUnexpected()
    };
}
