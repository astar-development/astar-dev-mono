using AStarDev.FunctionalParadigm;
using static AStarDev.CloudSync.Sync.TestsUnit.UploadPlanFixture;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenUploadPassTypeFactories
{
    [Fact]
    public void when_upload_targets_are_created_then_each_keeps_its_parent_or_item()
    {
        var inFolder = UploadTargetFactory.CreateNewFileIn(Option.Some(Id("p1")));
        var inRoot = UploadTargetFactory.CreateNewFileIn(Option.None<RemoteItemId>());
        var replacing = UploadTargetFactory.CreateReplacingItem(Id("r1"));

        (inFolder.ShouldBeOfType<UploadTarget.NewFileIn>().Parent.Match(id => id.Value, () => "root"), inRoot.ShouldBeOfType<UploadTarget.NewFileIn>().Parent.Match(id => id.Value, () => "root"), replacing.ShouldBeOfType<UploadTarget.ReplacingItem>().ItemId.Value).ShouldBe(("p1", "root", "r1"));
    }

    [Fact]
    public void when_the_default_options_are_created_then_eight_uploads_may_run_at_once()
    {
        UploadPassOptionsFactory.CreateDefault().MaxConcurrentUploads.ShouldBe(8);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    [InlineData(3, 3)]
    public void when_options_are_created_with_a_concurrency_then_it_is_at_least_one(int requested, int expected)
    {
        UploadPassOptionsFactory.Create(requested).MaxConcurrentUploads.ShouldBe(expected);
    }

    [Fact]
    public void when_a_content_remove_precondition_is_created_then_it_keeps_the_tag_and_a_blank_tag_is_rejected()
    {
        var created = RemovePreconditionFactory.CreateMustMatchContent(new ContentTag("c1"));
        var blank = RemovePreconditionFactory.CreateMustMatchContent(new ContentTag("  "));

        (created.Match(found => found.ShouldBeOfType<RemovePrecondition.MustMatchContent>().CTag.Value, error => error.Message), blank.Match(_ => "created", _ => "rejected")).ShouldBe(("c1", "rejected"));
    }

    [Fact]
    public void when_a_source_changed_error_is_created_then_it_is_its_own_case()
    {
        ProviderErrorFactory.CreateSourceChanged().ShouldBeOfType<ProviderError.SourceChanged>();
    }

    [Fact]
    public void when_an_uploaded_file_is_created_then_it_keeps_the_hash_and_a_negative_size_becomes_zero()
    {
        var uploaded = UploadedFileFactory.Create(Id("i"), new ContentTag("c"), new EntityTag("e"), -5, Hash("abc"));

        (uploaded.Hash.Value, uploaded.Size).ShouldBe(("abc", 0L));
    }

    [Fact]
    public void when_every_upload_outcome_is_created_then_each_keeps_its_path()
    {
        var path = At("a.txt");
        UploadItemOutcome[] outcomes = [UploadItemOutcomeFactory.CreateUploaded(path, 1), UploadItemOutcomeFactory.CreateFolderCreated(path), UploadItemOutcomeFactory.CreateLinked(path), UploadItemOutcomeFactory.CreateRemovedRemotely(path), UploadItemOutcomeFactory.CreateDeferred(path)];

        outcomes.ShouldAllBe(outcome => outcome.Path.Equals(path));
    }
}
