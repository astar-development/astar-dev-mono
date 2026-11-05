using AStarDev.CloudSync.Core.Accounts;
using AStarDev.CloudSync.Infrastructure.OneDrive;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Infrastructure.TestsUnit.OneDrive;

public sealed class FakeDriveLookup : IDriveLookup
{
    private readonly List<string> receivedTokens = [];

    public Func<CancellationToken, Task<Result<MicrosoftDrive, AccountAddFailure>>> Lookup { get; init; } = _ => Task.FromResult(Result.Success<MicrosoftDrive, AccountAddFailure>(TestSignIns.Drive));

    public IReadOnlyList<string> ReceivedTokens => receivedTokens;

    public Task<Result<MicrosoftDrive, AccountAddFailure>> GetDriveAsync(string accessToken, CancellationToken cancellationToken)
    {
        receivedTokens.Add(accessToken);

        return Lookup(cancellationToken);
    }
}
