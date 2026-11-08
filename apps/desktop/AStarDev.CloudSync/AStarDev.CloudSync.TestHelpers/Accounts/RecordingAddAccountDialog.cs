using AStarDev.CloudSync.Accounts;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class RecordingAddAccountDialog : IAddAccountDialog
{
    private readonly List<CancellationToken> receivedTokens = [];

    public IReadOnlyList<CancellationToken> ReceivedTokens => receivedTokens;

    public Task ShowAsync(CancellationToken cancellationToken)
    {
        receivedTokens.Add(cancellationToken);

        return Task.CompletedTask;
    }
}
