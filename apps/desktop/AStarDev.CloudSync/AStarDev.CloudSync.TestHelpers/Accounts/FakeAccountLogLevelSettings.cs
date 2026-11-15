using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestHelpers.Accounts;

public sealed class FakeAccountLogLevelSettings : IAccountLogLevelSettings
{
    private readonly Dictionary<AccountId, AccountLogLevel> levels = [];
    private readonly List<(AccountId AccountId, AccountLogLevel Level)> saved = [];

    public bool ReadFails { get; set; }

    public bool WriteFails { get; set; }

    public IReadOnlyList<(AccountId AccountId, AccountLogLevel Level)> Saved => saved;

    public void Seed(AccountId accountId, AccountLogLevel level) => levels[accountId] = level;

    public Task<Result<AccountLogLevel, ErrorResponse>> GetAsync(AccountId accountId, CancellationToken cancellationToken) =>
        Task.FromResult(ReadFails
            ? Result.Failure<AccountLogLevel, ErrorResponse>(new ErrorResponse("locked"))
            : Result.Success<AccountLogLevel, ErrorResponse>(levels.GetValueOrDefault(accountId, AccountLogLevelFactory.CreateStandard())));

    public Task<Result<Unit, ErrorResponse>> SetAsync(AccountId accountId, AccountLogLevel level, CancellationToken cancellationToken)
    {
        if (WriteFails)
            return Task.FromResult(Result.Failure<Unit, ErrorResponse>(new ErrorResponse("locked")));

        saved.Add((accountId, level));
        levels[accountId] = level;

        return Task.FromResult(Result.Success<Unit, ErrorResponse>(Unit.Instance));
    }

    public Task<Result<IReadOnlyList<AccountId>, ErrorResponse>> ListVerboseAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success<IReadOnlyList<AccountId>, ErrorResponse>([.. levels.Where(pair => pair.Value is AccountLogLevel.Verbose).Select(pair => pair.Key)]));
}
