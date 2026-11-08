using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.TestHelpers.Accounts;
using AStarDev.CloudSync.TestHelpers.Common;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.CloudSync.Accounts.OneDrive.TestsUnit;

public sealed class OneDriveFakes
{
    private FakeHttpClientFactory? httpClientFactory;

    public FakeMsalTokenCacheStorage Storage { get; init; } = new();

    public FakeTokenCacheConsent Consent { get; init; } = new();

    public FakeMsalClient Msal { get; init; } = new();

    public FakeGraphHandler Graph { get; init; } = new();

    public IAccountStore AccountStore { get; init; } = new FakeAccountStore();

    public ConsentFile ConsentFile { get; init; } = new();

    public TimeProvider Time { get; init; } = new FakeTimeProvider(TestClock.Now);

    public RecordingLoggerProvider Logs { get; init; } = new();

    public FakeHttpClientFactory HttpClientFactory => httpClientFactory ??= new FakeHttpClientFactory(Graph);
}
