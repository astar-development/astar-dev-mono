using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Ui;
using AStarDev.CloudSync.ViewModels.Accounts;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.TestsUnit.Accounts;

public class GivenAnAccountDialogEditingTransferConcurrency
{
    private readonly FakeTransferConcurrency concurrency = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string Text(string key, params object[] arguments) => TestData.CreateLocalization().GetLocal(key, arguments);

    private (AccountDialogViewModel Dialog, AccountViewModel Account) Create()
    {
        var dialog = TestData.CreateAccountDialog(new FakeAccountActions(), new FakeSyncRootReader(), new InMemoryAccountStore(), new FakeFolderPicker(), concurrency, concurrency);

        return (dialog, TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), dialog));
    }

    private static async Task OpenAsync(AccountDialogViewModel dialog, AccountViewModel account)
    {
        _ = dialog.ChangeSyncFolderAsync(account, CancellationToken);
        await Task.Delay(50, CancellationToken);
    }

    [Fact]
    public async Task when_the_sync_folder_panel_opens_then_it_shows_the_accounts_saved_value()
    {
        concurrency.Stored = 6;
        var (dialog, account) = Create();

        await OpenAsync(dialog, account);

        dialog.TransferConcurrencyText.ShouldBe(Text("AccountDialog.Transfers.Current", "6"));
    }

    [Fact]
    public async Task when_the_value_is_raised_then_it_is_saved_for_that_account_and_shown()
    {
        var (dialog, account) = Create();
        await OpenAsync(dialog, account);

        await dialog.IncreaseTransferConcurrencyCommand.ExecuteAsync(null);

        concurrency.Saved.ShouldBe([(account.Id, 5)]);
        dialog.TransferConcurrencyText.ShouldBe(Text("AccountDialog.Transfers.Current", "5"));
    }

    [Fact]
    public async Task when_the_value_is_lowered_then_it_is_saved_and_shown()
    {
        concurrency.Stored = 3;
        var (dialog, account) = Create();
        await OpenAsync(dialog, account);

        await dialog.DecreaseTransferConcurrencyCommand.ExecuteAsync(null);

        (concurrency.Saved.Single().Value, dialog.TransferConcurrencyText).ShouldBe((2, Text("AccountDialog.Transfers.Current", "2")));
    }

    [Fact]
    public async Task when_the_value_is_at_the_top_then_it_cannot_be_raised_and_at_the_bottom_it_cannot_be_lowered()
    {
        concurrency.Stored = TransferConcurrencyFactory.Maximum;
        var (top, topAccount) = Create();
        await OpenAsync(top, topAccount);
        var other = new FakeTransferConcurrency { Stored = TransferConcurrencyFactory.Minimum };
        var bottom = TestData.CreateAccountDialog(new FakeAccountActions(), new FakeSyncRootReader(), new InMemoryAccountStore(), new FakeFolderPicker(), other, other);
        await OpenAsync(bottom, TestData.CreateAccountViewModel(TestData.CreateAccountNeedingSignIn(), bottom));

        (top.IncreaseTransferConcurrencyCommand.CanExecute(null), top.DecreaseTransferConcurrencyCommand.CanExecute(null), bottom.IncreaseTransferConcurrencyCommand.CanExecute(null), bottom.DecreaseTransferConcurrencyCommand.CanExecute(null)).ShouldBe((false, true, true, false));
    }

    [Fact]
    public async Task when_saving_fails_then_the_value_stays_and_a_message_says_so()
    {
        var (dialog, account) = Create();
        await OpenAsync(dialog, account);
        concurrency.WriteFails = true;

        await dialog.IncreaseTransferConcurrencyCommand.ExecuteAsync(null);

        (dialog.TransferConcurrencyText, dialog.TransferConcurrencyMessageText).ShouldBe((Text("AccountDialog.Transfers.Current", "4"), Text("AccountDialog.Transfers.SaveFailed")));
    }

    [Fact]
    public async Task when_the_saved_value_cannot_be_read_then_the_default_is_shown()
    {
        concurrency.ReadFails = true;
        var (dialog, account) = Create();

        await OpenAsync(dialog, account);

        dialog.TransferConcurrencyText.ShouldBe(Text("AccountDialog.Transfers.Current", "4"));
    }

    [Fact]
    public async Task when_the_help_is_read_then_it_explains_the_trade_off()
    {
        var (dialog, account) = Create();
        await OpenAsync(dialog, account);

        dialog.TransferConcurrencyHelpText.ShouldBe(Text("AccountDialog.Transfers.Help"));
    }

    [Fact]
    public async Task when_the_dialog_is_closed_then_the_buttons_cannot_run()
    {
        var (dialog, _) = Create();
        await Task.CompletedTask;

        (dialog.IncreaseTransferConcurrencyCommand.CanExecute(null), dialog.DecreaseTransferConcurrencyCommand.CanExecute(null)).ShouldBe((false, false));
    }
}
