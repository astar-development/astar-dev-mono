using AStar.Dev.OneDrive.Sync.Client.Activity;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Activity;

public sealed class GivenAnActivityItemViewModelFactory
{
    private static ActivityItemViewModelFactory CreateSut()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetLocal(Arg.Any<string>()).Returns(callInfo => (string)callInfo[0]);

        return new ActivityItemViewModelFactory(localizationService);
    }

    private static SyncJob BuildDownloadJob()
    {
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId(string.Empty), new OneDriveItemId(string.Empty));
        var target = SyncFileTargetFactory.Create(string.Empty, "Documents/report.pdf");
        var metadata = SyncFileMetadataFactory.Create(2048L, default);

        return SyncJobFactory.CreateDownload(remote, target, metadata);
    }

    [Fact]
    public void when_create_info_is_called_then_an_info_item_is_returned_for_the_account()
    {
        var sut = CreateSut();

        var item = sut.CreateInfo("account-123", "report.pdf");

        item.AccountId.ShouldBe("account-123");
        item.FileName.ShouldBe("report.pdf");
        item.Type.ShouldBe(ActivityItemType.Info);
    }

    [Fact]
    public void when_create_is_called_then_an_item_with_only_the_file_name_is_returned()
    {
        var sut = CreateSut();

        var item = sut.Create("Sync starting");

        item.FileName.ShouldBe("Sync starting");
        item.AccountId.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_create_from_job_is_called_then_the_job_details_are_projected_onto_the_item()
    {
        var sut = CreateSut();

        var item = sut.CreateFromJob(BuildDownloadJob(), "user@example.com");

        item.AccountId.ShouldBe("account-123");
        item.AccountEmail.ShouldBe("user@example.com");
        item.FileName.ShouldBe("report.pdf");
        item.Type.ShouldBe(ActivityItemType.Downloaded);
    }
}
