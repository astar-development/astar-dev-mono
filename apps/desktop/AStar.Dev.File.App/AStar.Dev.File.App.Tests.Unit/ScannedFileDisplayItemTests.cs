using AStar.Dev.File.App.Models;
using AStar.Dev.File.App.ViewModels;

namespace AStar.Dev.File.App.Tests.Unit;

public class ScannedFileDisplayItemTests
{
    [Fact]
    public void FullPath_IsPopulatedFromScannedFile()
    {
        var file = MakeFile(fullPath: "/data/photos/sunset.jpg");
        var sut = new ScannedFileDisplayItem(file);
        sut.FullPath.ShouldBe("/data/photos/sunset.jpg");
    }

    [Theory]
    [InlineData("sunset.jpg", "JPG")]
    [InlineData("report.pdf", "PDF")]
    [InlineData("noextension", "")]
    public void Extension_IsUppercaseExtensionWithoutDot(string fileName, string expected)
    {
        var file = MakeFile(fileName: fileName);
        var sut = new ScannedFileDisplayItem(file);
        sut.Extension.ShouldBe(expected);
    }

    [Theory]
    [InlineData(FileType.Image, true)]
    [InlineData(FileType.Document, false)]
    [InlineData(FileType.Unknown, false)]
    public void IsImage_IsTrueOnlyForImageFileType(FileType fileType, bool expected)
    {
        var file = MakeFile(fileType: fileType);
        var sut = new ScannedFileDisplayItem(file);
        sut.IsImage.ShouldBe(expected);
    }

    [Fact]
    public void SizeInBytes_IsMappedFromScannedFile()
    {
        var file = MakeFile(sizeInBytes: 12345);
        var sut = new ScannedFileDisplayItem(file);
        sut.SizeInBytes.ShouldBe(12345);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PendingDelete_IsMappedFromScannedFile(bool pendingDelete)
    {
        var file = MakeFile(pendingDelete: pendingDelete);
        var sut = new ScannedFileDisplayItem(file);
        sut.PendingDelete.ShouldBe(pendingDelete);
    }

    private static ScannedFile MakeFile(
        string fullPath = "/data/docs/file.txt",
        string fileName = "file.txt",
        FileType fileType = FileType.Unknown,
        int id = 0,
        bool pendingDelete = false,
        long sizeInBytes = 0) => new()
        {
            Id = id,
            RootPath = "/data",
            FolderPath = "/data/docs",
            FileName = fileName,
            FullPath = fullPath,
            FileType = fileType,
            PendingDelete = pendingDelete,
            SizeInBytes = sizeInBytes,
            LastModified = DateTime.UtcNow
        };

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(500, "500 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1_048_575, "1024.0 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(1_572_864, "1.5 MB")]
    [InlineData(1_073_741_823, "1024.0 MB")]
    [InlineData(1_073_741_824L, "1.0 GB")]
    [InlineData(1_610_612_736L, "1.5 GB")]
    public void FormatSize_ReturnsExpectedString(long bytes, string expected)
        => ScannedFileDisplayItem.FormatSize(bytes).ShouldBe(expected);
}
