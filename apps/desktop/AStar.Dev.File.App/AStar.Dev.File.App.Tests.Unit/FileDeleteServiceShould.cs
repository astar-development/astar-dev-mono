using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.Tests.Unit;

public class FileDeleteServiceShould
{
    private readonly FileDeleteService _sut = new();

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ReturnWithoutError()
    {
        const string filePath = "/nonexistent/path/file.txt";

        await _sut.DeleteFileAsync(filePath, moveToRecycleBin: false);
    }

    [Fact]
    public async Task DeleteFileAsync_WithNullFilePath_ReturnWithoutError()
    {
        await _sut.DeleteFileAsync(null!, moveToRecycleBin: false);
    }

    [Fact]
    public async Task DeleteFileAsync_WithEmptyFilePath_ReturnWithoutError()
    {
        await _sut.DeleteFileAsync(string.Empty, moveToRecycleBin: false);
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_DeletesPermanentlyWhenNotRecycling()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            System.IO.File.Exists(tempFile).ShouldBeTrue();

            await _sut.DeleteFileAsync(tempFile, moveToRecycleBin: false);

            System.IO.File.Exists(tempFile).ShouldBeFalse();
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeleteFilesAsync_WithEmptyList_ReturnWithoutError()
    {
        await _sut.DeleteFilesAsync([], moveToRecycleBin: false);
    }

    [Fact]
    public async Task DeleteFilesAsync_WithNonExistentFiles_ReturnWithoutError()
    {
        string[] files = ["/nonexistent/file1.txt", "/nonexistent/file2.txt"];

        await _sut.DeleteFilesAsync(files, moveToRecycleBin: false);
    }

    [Fact]
    public async Task DeleteFilesAsync_WithExistingFiles_DeletesPermanentlyWhenNotRecycling()
    {
        string tempFile1 = Path.GetTempFileName();
        string tempFile2 = Path.GetTempFileName();

        try
        {
            System.IO.File.Exists(tempFile1).ShouldBeTrue();
            System.IO.File.Exists(tempFile2).ShouldBeTrue();

            await _sut.DeleteFilesAsync([tempFile1, tempFile2], moveToRecycleBin: false);

            System.IO.File.Exists(tempFile1).ShouldBeFalse();
            System.IO.File.Exists(tempFile2).ShouldBeFalse();
        }
        finally
        {
            if (System.IO.File.Exists(tempFile1))
                System.IO.File.Delete(tempFile1);
            if (System.IO.File.Exists(tempFile2))
                System.IO.File.Delete(tempFile2);
        }
    }

    [Fact]
    public async Task DeleteFilesAsync_WithMixedExistingAndNonExistentFiles_DeletesOnlyExistingFiles()
    {
        string tempFile = Path.GetTempFileName();
        const string nonExistentFile = "/nonexistent/file.txt";

        try
        {
            System.IO.File.Exists(tempFile).ShouldBeTrue();

            await _sut.DeleteFilesAsync([tempFile, nonExistentFile], moveToRecycleBin: false);

            System.IO.File.Exists(tempFile).ShouldBeFalse();
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeleteFileAsync_WithMultipleCallsOnSameFile_HandlesGracefully()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            await _sut.DeleteFileAsync(tempFile, moveToRecycleBin: false);
            System.IO.File.Exists(tempFile).ShouldBeFalse();

            await _sut.DeleteFileAsync(tempFile, moveToRecycleBin: false);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeleteFileAsync_WithWhitespaceFilePath_ReturnWithoutError()
    {
        await _sut.DeleteFileAsync("   ", moveToRecycleBin: false);
    }

    [Fact]
    public async Task DeleteFilesAsync_WithRecycleBinFlag_DoesNotThrowOnUnsupportedPlatform()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            await _sut.DeleteFilesAsync([tempFile], moveToRecycleBin: true);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }
}
