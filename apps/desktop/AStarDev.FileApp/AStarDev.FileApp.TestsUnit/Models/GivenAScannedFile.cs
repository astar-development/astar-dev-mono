using AStar.Dev.File.App.Models;

namespace AStar.Dev.File.App.TestsUnit.Models;

public class GivenAScannedFile
{
    [Fact]
    public void when_constructed_with_required_members_then_properties_round_trip()
    {
        var lastModified = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var lastScannedAt = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        var sut = new ScannedFile
        {
            Id = 42,
            RootPath = "/data",
            FolderPath = "/data/docs",
            FileName = "report.pdf",
            FullPath = "/data/docs/report.pdf",
            LastModified = lastModified,
            SizeInBytes = 2048,
            FileType = FileType.Document,
            LastViewed = null,
            PendingDelete = true,
            LastScannedAt = lastScannedAt
        };

        sut.Id.ShouldBe(42);
        sut.RootPath.ShouldBe("/data");
        sut.FolderPath.ShouldBe("/data/docs");
        sut.FileName.ShouldBe("report.pdf");
        sut.FullPath.ShouldBe("/data/docs/report.pdf");
        sut.LastModified.ShouldBe(lastModified);
        sut.SizeInBytes.ShouldBe(2048);
        sut.FileType.ShouldBe(FileType.Document);
        sut.LastViewed.ShouldBeNull();
        sut.PendingDelete.ShouldBeTrue();
        sut.LastScannedAt.ShouldBe(lastScannedAt);
    }

    [Fact]
    public void when_constructed_without_optional_members_then_defaults_are_applied()
    {
        var sut = new ScannedFile
        {
            RootPath = string.Empty,
            FolderPath = string.Empty,
            FileName = string.Empty,
            FullPath = string.Empty
        };

        sut.Id.ShouldBe(0);
        sut.SizeInBytes.ShouldBe(0);
        sut.FileType.ShouldBe(FileType.Image);
        sut.LastViewed.ShouldBeNull();
        sut.PendingDelete.ShouldBeFalse();
    }
}
