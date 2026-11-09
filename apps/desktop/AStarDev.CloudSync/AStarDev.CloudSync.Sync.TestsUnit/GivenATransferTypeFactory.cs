using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenATransferTypeFactory
{
    [Theory]
    [InlineData(50, 100, 50, 100)]
    [InlineData(150, 100, 100, 100)]
    [InlineData(-5, -1, 0, 0)]
    [InlineData(500, 0, 500, 0)]
    public void when_progress_is_created_then_it_is_normalised(long done, long total, long expectedDone, long expectedTotal)
    {
        var progress = TransferProgressFactory.Create(done, total);

        (progress.BytesDone, progress.BytesTotal).ShouldBe((expectedDone, expectedTotal));
    }

    [Fact]
    public void when_a_download_result_has_a_negative_size_or_no_ctag_then_they_are_normalised()
    {
        var downloaded = DownloadedFileFactory.Create(RemoteIds.Of("file-1"), null, -1, Option.Some("hash"));

        (downloaded.Id, downloaded.CTag, downloaded.Size, downloaded.Hash).ShouldBe((RemoteIds.Of("file-1"), string.Empty, 0L, Option.Some("hash")));
    }

    [Fact]
    public void when_an_upload_result_has_missing_tags_then_they_are_empty()
    {
        var uploaded = UploadedFileFactory.Create(RemoteIds.Of("file-1"), null, null, 2_048);

        (uploaded.CTag, uploaded.ETag, uploaded.Size).ShouldBe((string.Empty, string.Empty, 2_048L));
    }

    [Fact]
    public void when_preconditions_are_created_then_they_keep_their_tags()
    {
        var replace = UploadPreconditionFactory.CreateMustMatch("ctag-1");
        var remove = RemovePreconditionFactory.CreateMustMatch("etag-1");

        (UploadPreconditionFactory.CreateMustNotExist() is UploadPrecondition.MustNotExist, replace.ShouldBeOfType<UploadPrecondition.MustMatch>().CTag, remove.ShouldBeOfType<RemovePrecondition.MustMatch>().ETag).ShouldBe((true, "ctag-1", "etag-1"));
    }

    [Fact]
    public void when_each_reauth_failure_is_created_then_it_is_that_case()
    {
        var failures = new[] { ReauthFailureFactory.CreateCancelled(), ReauthFailureFactory.CreateDifferentAccount(), ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()) };

        failures.Select(failure => failure.GetType().Name).ShouldBe(["Cancelled", "DifferentAccount", "Provider"]);
        failures[2].ShouldBeOfType<ReauthFailure.Provider>().Error.ShouldBe(ProviderErrorFactory.CreateTransient());
    }
}
