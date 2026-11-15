using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using static AStarDev.CloudSync.Sync.TestsUnit.DownloadPassFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAVerboseDownloadPass
{
    private static readonly IReadOnlyList<string> PersonalValues = PersonalData.All;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_a_download_pass_downloads_fails_and_trashes_over_personal_names_then_nothing_personal_is_in_any_log_entry()
    {
        using var fixture = await CreateAsync(CancellationToken);
        fixture.Docs();
        _ = fixture.SyncedFolder("Docs/SecretClientFolder", "secret-id");
        fixture.Remote.Folder("Docs");
        fixture.Remote.Folder("Docs/SecretClientFolder");
        fixture.Remote.File($"Docs/SecretClientFolder/{PersonalData.DisplayName} passport-scan {PersonalData.Email} {PersonalData.AccessToken}.txt", PersonalData.DownloadUrl, "c1", 5);
        fixture.Remote.File($"Docs/SecretClientFolder/passport-scan {PersonalData.DisplayName} {PersonalData.RefreshToken} {PersonalData.BearerToken} refused.txt", PersonalData.UploadUrl, "c2", 5);
        fixture.Downloader.Content[PersonalData.DownloadUrl] = "hello"u8.ToArray();
        fixture.Downloader.Failures[PersonalData.UploadUrl] = ProviderErrorFactory.CreateTransient();
        _ = fixture.Synced("Docs/SecretClientFolder/Jane Doe deleted-remotely.txt", "gone", "gone");

        _ = await fixture.RunAsync(CancellationToken);

        var logged = LoggedText.From(fixture.Logs).ToList();
        logged.ShouldNotBeEmpty();
        LoggedText.Personal(logged, PersonalValues).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_personal_value_is_logged_by_a_download_pass_then_the_guard_sees_it()
    {
        using var logs = new TestHelpers.Common.RecordingLoggerProvider();
        logs.Logger.Log(LogLevel.Information, new EventId(1), "Downloading passport-scan.txt", null, (state, _) => state);

        LoggedText.Personal(LoggedText.From(logs), PersonalValues).ShouldNotBeEmpty();
    }
}
