using AStar.Dev.File.App.Services;

namespace AStar.Dev.File.App.TestsUnit;

public class GivenAFileDeleteService
{
    private readonly FileDeleteService _sut = new();

    [Fact]
    public async Task when_deleting_a_nonexistent_file_then_returns_without_error()
    {
        const string filePath = "/nonexistent/path/file.txt";

        await Should.NotThrowAsync(() => _sut.DeleteFileAsync(filePath, moveToRecycleBin: false));
    }

    [Fact]
    public async Task when_deleting_a_null_file_path_then_returns_without_error()
    {
        await Should.NotThrowAsync(() => _sut.DeleteFileAsync(null!, moveToRecycleBin: false));
    }

    [Fact]
    public async Task when_deleting_an_empty_file_path_then_returns_without_error()
    {
        await Should.NotThrowAsync(() => _sut.DeleteFileAsync(string.Empty, moveToRecycleBin: false));
    }

    [Fact]
    public async Task when_deleting_an_existing_file_without_recycling_then_file_is_permanently_deleted()
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
    public async Task when_deleting_an_empty_list_of_files_then_returns_without_error()
    {
        await Should.NotThrowAsync(() => _sut.DeleteFilesAsync([], moveToRecycleBin: false));
    }

    [Fact]
    public async Task when_deleting_nonexistent_files_then_returns_without_error()
    {
        string[] files = ["/nonexistent/file1.txt", "/nonexistent/file2.txt"];

        await Should.NotThrowAsync(() => _sut.DeleteFilesAsync(files, moveToRecycleBin: false));
    }

    [Fact]
    public async Task when_deleting_existing_files_without_recycling_then_files_are_permanently_deleted()
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
    public async Task when_deleting_mixed_existing_and_nonexistent_files_then_only_existing_files_are_deleted()
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
    public async Task when_deleting_the_same_file_multiple_times_then_handles_gracefully()
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
    public async Task when_deleting_a_whitespace_file_path_then_returns_without_error()
    {
        await Should.NotThrowAsync(() => _sut.DeleteFileAsync("   ", moveToRecycleBin: false));
    }

    [Fact]
    public async Task when_deleting_files_with_recycle_bin_flag_then_does_not_throw_on_unsupported_platform()
    {
        string tempFile = Path.GetTempFileName();

        try
        {
            await Should.NotThrowAsync(() => _sut.DeleteFilesAsync([tempFile], moveToRecycleBin: true));
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }
}
