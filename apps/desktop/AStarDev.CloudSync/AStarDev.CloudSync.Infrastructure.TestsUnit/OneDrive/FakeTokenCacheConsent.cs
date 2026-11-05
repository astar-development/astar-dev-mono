using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeTokenCacheConsent : ITokenCacheConsent
{
    private int questionsAsked;

    public PlaintextCacheConsent Answer { get; set; } = PlaintextCacheConsentFactory.CreateDeclined();

    public int QuestionsAsked => questionsAsked;

    public Task<PlaintextCacheConsent> AskAsync(CancellationToken cancellationToken)
    {
        _ = Interlocked.Increment(ref questionsAsked);

        return Task.FromResult(Answer);
    }
}
