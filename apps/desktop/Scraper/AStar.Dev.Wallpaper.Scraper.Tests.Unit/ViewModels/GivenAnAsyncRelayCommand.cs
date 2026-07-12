using AStar.Dev.Wallpaper.Scraper.ViewModels;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.ViewModels;

public sealed class GivenAnAsyncRelayCommand
{
    [Fact]
    public async Task when_execute_is_called_with_cancellable_delegate_then_cancellation_token_is_passed()
    {
        var tokenReceived = CancellationToken.None;
        var expectedToken = new CancellationTokenSource().Token;
        var command = new AsyncRelayCommand(async token =>
        {
            tokenReceived = token;
            await Task.CompletedTask;
        });

        await command.ExecuteAsync(expectedToken);

        tokenReceived.ShouldBe(expectedToken);
    }

    [Fact]
    public async Task when_execute_is_called_with_cancelled_token_then_operation_is_cancelled()
    {
        var executed = false;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var command = new AsyncRelayCommand(async token =>
        {
            token.ThrowIfCancellationRequested();
            executed = true;
            await Task.CompletedTask;
        });

        await Should.ThrowAsync<OperationCanceledException>(async () => await command.ExecuteAsync(cts.Token));

        executed.ShouldBeFalse();
    }

    [Fact]
    public void when_can_execute_is_called_while_executing_then_returns_false()
    {
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async _ => await tcs.Task);

        _ = command.ExecuteAsync(CancellationToken.None);
        var canExecute = command.CanExecute(null);

        canExecute.ShouldBeFalse();
        tcs.SetResult();
    }
}
