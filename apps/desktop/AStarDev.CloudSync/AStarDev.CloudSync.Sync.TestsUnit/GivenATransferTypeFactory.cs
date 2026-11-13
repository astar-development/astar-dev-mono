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
    public void when_a_download_result_has_a_negative_size_then_the_size_is_zero()
    {
        var downloaded = DownloadedFileFactory.Create(RemoteIds.Of("file-1"), RemoteIds.CTag("ctag"), -1, Option.Some(RemoteIds.Hash("hash")));

        (downloaded.Id, downloaded.CTag, downloaded.Size, downloaded.Hash).ShouldBe((RemoteIds.Of("file-1"), RemoteIds.CTag("ctag"), 0L, Option.Some(RemoteIds.Hash("hash"))));
    }

    [Fact]
    public void when_an_upload_result_is_created_then_it_keeps_both_tags()
    {
        var uploaded = UploadedFileFactory.Create(RemoteIds.Of("file-1"), RemoteIds.CTag("ctag"), RemoteIds.ETag("etag"), 2_048, ContentHashFactory.Create("abc").Match(hash => hash, _ => throw new InvalidOperationException()));

        (uploaded.CTag, uploaded.ETag, uploaded.Size).ShouldBe((RemoteIds.CTag("ctag"), RemoteIds.ETag("etag"), 2_048L));
    }

    [Fact]
    public void when_preconditions_are_created_then_they_keep_their_tags()
    {
        var replace = UploadPreconditionFactory.CreateMustMatch(RemoteIds.CTag("ctag-1"));
        var remove = RemovePreconditionFactory.CreateMustMatch(RemoteIds.ETag("etag-1"));

        (UploadPreconditionFactory.CreateMustNotExist() is UploadPrecondition.MustNotExist, replace.Match(found => found.ShouldBeOfType<UploadPrecondition.MustMatch>().CTag.Value, error => error.Message), remove.Match(found => found.ShouldBeOfType<RemovePrecondition.MustMatch>().ETag.Value, error => error.Message)).ShouldBe((true, "ctag-1", "etag-1"));
    }

    [Fact]
    public void when_a_precondition_tag_is_blank_then_the_precondition_is_rejected()
    {
        ContentTag blankContent = "  ";
        EntityTag blankEntity = default;

        (UploadPreconditionFactory.CreateMustMatch(blankContent).Match(_ => "created", _ => "rejected"), RemovePreconditionFactory.CreateMustMatch(blankEntity).Match(_ => "created", _ => "rejected")).ShouldBe(("rejected", "rejected"));
    }

    [Fact]
    public void when_each_reauth_failure_is_created_then_it_is_that_case()
    {
        var failures = new[] { ReauthFailureFactory.CreateCancelled(), ReauthFailureFactory.CreateDifferentAccount(), ReauthFailureFactory.CreateProvider(ProviderErrorFactory.CreateTransient()) };

        failures.Select(failure => failure.GetType().Name).ShouldBe(["Cancelled", "DifferentAccount", "Provider"]);
        failures[2].ShouldBeOfType<ReauthFailure.Provider>().Error.ShouldBe(ProviderErrorFactory.CreateTransient());
    }
}
