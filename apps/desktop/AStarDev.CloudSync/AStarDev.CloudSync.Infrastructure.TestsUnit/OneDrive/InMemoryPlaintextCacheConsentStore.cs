using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class InMemoryPlaintextCacheConsentStore : IPlaintextCacheConsentStore
{
    private readonly List<PlaintextCacheConsent> savedAnswers = [];

    public Option<PlaintextCacheConsent> Remembered { get; set; } = Option.None<PlaintextCacheConsent>();

    public IReadOnlyList<PlaintextCacheConsent> SavedAnswers => savedAnswers;

    public bool SaveFails { get; init; }

    public Option<PlaintextCacheConsent> Load() => Remembered;

    public Result<Unit, ErrorResponse> Save(PlaintextCacheConsent consent)
    {
        if (SaveFails)
            return Result.Failure<Unit, ErrorResponse>(new ErrorResponse("The consent file is read-only."));

        savedAnswers.Add(consent);
        Remembered = consent is PlaintextCacheConsent.Granted ? Option.Some(consent) : Option.None<PlaintextCacheConsent>();

        return Result.Success<Unit, ErrorResponse>(Unit.Instance);
    }
}
