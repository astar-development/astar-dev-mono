using AStar.Dev.OneDrive.Client.Authentication;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Authentication;

[TestSubject(typeof(FileConsentStore))]
public sealed class FileConsentStoreShould : IDisposable
{
    private readonly string _testDirectory =
        Path.Combine(Path.GetTempPath(), $"astar-consent-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ReturnFalse_WhenNoConsentHasBeenRecordedForTheAccount()
    {
        var sut = new FileConsentStore(_testDirectory);

        sut.HasConsented("account-123").ShouldBeFalse();
    }

    [Fact]
    public void ReturnTrue_AfterConsentIsRecorded()
    {
        var sut = new FileConsentStore(_testDirectory);

        sut.RecordConsent("account-123", consented: true);

        sut.HasConsented("account-123").ShouldBeTrue();
    }

    [Fact]
    public void ReturnFalse_AfterConsentIsDenied()
    {
        var sut = new FileConsentStore(_testDirectory);

        sut.RecordConsent("account-123", consented: false);

        sut.HasConsented("account-123").ShouldBeFalse();
    }

    [Fact]
    public void TrackConsentIndependentlyPerAccount()
    {
        var sut = new FileConsentStore(_testDirectory);

        sut.RecordConsent("account-A", consented: true);
        sut.RecordConsent("account-B", consented: false);

        sut.HasConsented("account-A").ShouldBeTrue();
        sut.HasConsented("account-B").ShouldBeFalse();
    }

    [Fact]
    public void PersistConsentAcrossInstances()
    {
        var first = new FileConsentStore(_testDirectory);
        first.RecordConsent("account-123", consented: true);

        var second = new FileConsentStore(_testDirectory);

        second.HasConsented("account-123").ShouldBeTrue();
    }

    [Fact]
    public void AllowConsentToBeRevoked()
    {
        var sut = new FileConsentStore(_testDirectory);
        sut.RecordConsent("account-123", consented: true);

        sut.RecordConsent("account-123", consented: false);

        sut.HasConsented("account-123").ShouldBeFalse();
    }

    public void Dispose()
    {
        if(Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
