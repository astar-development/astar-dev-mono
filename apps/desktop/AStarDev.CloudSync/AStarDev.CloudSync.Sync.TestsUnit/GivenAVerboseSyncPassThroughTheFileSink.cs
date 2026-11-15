using AStarDev.CloudSync.Logging;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAVerboseSyncPassThroughTheFileSink : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"cloudsync-pii-{Guid.NewGuid():N}");
    private readonly AccountLogLevels levels = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    private string Written() => string.Join('\n', Directory.GetFiles(folder).Select(File.ReadAllText));

    [Fact]
    public async Task when_a_verbose_scan_and_upload_pass_run_over_personal_names_then_no_written_log_file_holds_a_personal_value()
    {
        using var scan = new LocalScanFixture(sink: logging => CloudSyncLogging.Configure(logging, levels, folder));
        using var upload = new UploadPassFixture { Sink = logging => CloudSyncLogging.Configure(logging, levels, folder) };
        levels.Apply(LocalScanFixture.Account, AccountLogLevelFactory.CreateVerbose());
        const string Folder = "Docs/SecretClientFolder";
        const string Passport = $"{Folder}/{PersonalData.DisplayName} passport-scan {PersonalData.Email} {PersonalData.AccessToken}.txt";
        const string Refused = $"{Folder}/passport-scan {PersonalData.RefreshToken} {PersonalData.BearerToken} refused.txt";
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
        levels.Apply(upload.Account, AccountLogLevelFactory.CreateVerbose());

        _ = await upload.RunAsync(CancellationToken);

        var written = Written();
        written.ShouldContain("[Information]");
        LoggedText.Personal([written], PersonalData.All).ShouldBeEmpty();
    }
}
