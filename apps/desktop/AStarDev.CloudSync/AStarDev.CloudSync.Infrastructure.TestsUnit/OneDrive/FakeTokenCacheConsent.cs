using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Domain.Accounts;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeTokenCacheConsent : ITokenCacheConsent
{
    private int questionsAsked;

    public PlaintextCacheConsent Answer { get; set; } = PlaintextCacheConsentFactory.CreateDeclined();

    public bool Fails { get; init; }

    public CancellationTokenSource? CancelOnFirstQuestion { get; init; }

    public TaskCompletionSource<PlaintextCacheConsent>? PendingAnswer { get; init; }

    public int QuestionsAsked => questionsAsked;

    public async Task<PlaintextCacheConsent> AskAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref questionsAsked) == 1 && CancelOnFirstQuestion is not null)
            await CancelOnFirstQuestion.CancelAsync();

        cancellationToken.ThrowIfCancellationRequested();

        if (PendingAnswer is not null)
            return await PendingAnswer.Task;

        return Fails ? throw new InvalidOperationException("The consent dialog could not open.") : Answer;
    }
}
