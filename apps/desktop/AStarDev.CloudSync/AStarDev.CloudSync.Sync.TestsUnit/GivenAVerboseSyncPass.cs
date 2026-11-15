using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAVerboseSyncPass : IDisposable
{
    private static readonly IReadOnlyList<string> PersonalValues = PersonalData.All;

    private readonly LocalScanFixture scan = new();
    private readonly UploadPassFixture upload = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        scan.Dispose();
        upload.Dispose();
    }

    private IEnumerable<string> EverythingLogged() => LoggedText.From(scan.Logs, upload.Logs);

    [Fact]
    public async Task when_a_scan_and_an_upload_pass_run_over_personal_names_then_nothing_personal_is_in_any_log_entry()
    {
        const string Folder = "Docs/SecretClientFolder";
        const string Passport = $"Docs/SecretClientFolder/{PersonalData.DisplayName} passport-scan {PersonalData.Email} {PersonalData.AccessToken}.txt";
        const string Refused = $"Docs/SecretClientFolder/passport-scan {PersonalData.DisplayName} {PersonalData.RefreshToken} {PersonalData.BearerToken} refused.txt";
        scan.Include("Docs");
        _ = scan.Write(Passport, "0123456789");
        _ = scan.Write(Refused, "0123456789");
        var changes = await scan.ScanAsync(CancellationToken);
        _ = upload.FileSystem.Directory.CreateDirectory($"/sync/{Folder}");
        upload.FileSystem.File.WriteAllText($"/sync/{Passport}", "0123456789");
        upload.FileSystem.File.WriteAllText($"/sync/{Refused}", "0123456789");
        upload.Uploader.Respond = name => name.Contains("refused", StringComparison.Ordinal)
            ? Result.Failure<UploadedFile, ProviderError>(ProviderErrorFactory.CreateTransient())
            : Result.Success<UploadedFile, ProviderError>(UploadedFileFactory.Create(UploadPlanFixture.Id($"{PersonalData.UploadUrl}#{name}"), new ContentTag("c"), new EntityTag("e"), 10, UploadPlanFixture.Hash("h")));
        _ = await upload.BaselineFolder("Docs", "docs-id").BaselineFolder(Folder, "secret-id").Changed(changes).BuildAsync(CancellationToken);

        _ = await upload.RunAsync(CancellationToken);

        var logged = EverythingLogged().ToList();
        logged.ShouldNotBeEmpty();
        LoggedText.Personal(logged, PersonalValues).ShouldBeEmpty();
    }

    [Fact]
    public void when_a_personal_value_is_logged_then_the_guard_sees_it()
    {
        scan.Logs.Logger.Log(LogLevel.Information, new EventId(1), "Uploading passport-scan.txt", null, (state, _) => state);

        LoggedText.Personal(EverythingLogged(), PersonalValues).ShouldNotBeEmpty();
    }
}
