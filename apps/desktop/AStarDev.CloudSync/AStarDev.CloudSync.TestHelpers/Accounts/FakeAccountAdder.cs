using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountAdder : IAccountAdder
{
    private readonly List<CancellationToken> receivedTokens = [];

    public Func<CancellationToken, Task<AccountAddOutcome>> Add { get; init; } = _ => Task.FromResult(AccountAddOutcomeFactory.CreateAdded(AccountIdFactory.Create(Guid.Parse("7d4c1a2b-3e5f-4a6b-8c9d-0e1f2a3b4c5d"))));

    public IReadOnlyList<CancellationToken> ReceivedTokens => receivedTokens;

    public Task<AccountAddOutcome> AddAsync(CancellationToken cancellationToken)
    {
        receivedTokens.Add(cancellationToken);

        return Add(cancellationToken);
    }
}
