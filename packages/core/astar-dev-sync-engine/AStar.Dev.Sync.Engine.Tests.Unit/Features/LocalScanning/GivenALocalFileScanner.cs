using System.IO.Abstractions.TestingHelpers;
using AStar.Dev.Sync.Engine.Features.LocalScanning;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Sync.Engine.Tests.Unit.Features.LocalScanning;

public sealed class GivenALocalFileScanner
{
    [Fact]
    public async Task when_directory_contains_normal_files_then_they_are_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/root/file1.txt"] = new("content1"),
            ["/root/file2.txt"] = new("content2"),
        });

        var sut = new LocalFileScanner(fs, NullLogger<LocalFileScanner>.Instance);

        var results = await sut.ScanAsync("/root", ct).ToListAsync(ct);

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task when_directory_contains_a_git_subdirectory_then_it_is_excluded()
    {
        var ct = TestContext.Current.CancellationToken;
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/root/normal.txt"] = new("content"),
            ["/root/.git/config"] = new("git config"),
            ["/root/.git/HEAD"] = new("ref: refs/heads/main"),
        });

        var sut = new LocalFileScanner(fs, NullLogger<LocalFileScanner>.Instance);

        var results = await sut.ScanAsync("/root", ct).ToListAsync(ct);

        results.ShouldHaveSingleItem();
        results[0].ShouldContain("normal.txt");
    }

    [Fact]
    public async Task when_root_is_a_git_directory_then_it_is_excluded()
    {
        var ct = TestContext.Current.CancellationToken;
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/root/.git/config"] = new("git config"),
        });

        var sut = new LocalFileScanner(fs, NullLogger<LocalFileScanner>.Instance);

        var results = await sut.ScanAsync("/root", ct).ToListAsync(ct);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_cancellation_is_requested_then_scan_stops()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["/root/a.txt"] = new("a"),
            ["/root/b.txt"] = new("b"),
            ["/root/c.txt"] = new("c"),
        });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var sut = new LocalFileScanner(fs, NullLogger<LocalFileScanner>.Instance);

#pragma warning disable xUnit1051
        await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.ScanAsync("/root", cts.Token).ToListAsync(cts.Token));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task when_directory_is_empty_then_no_files_are_returned()
    {
        var ct = TestContext.Current.CancellationToken;
        var fs = new MockFileSystem();
        fs.AddDirectory("/root");

        var sut = new LocalFileScanner(fs, NullLogger<LocalFileScanner>.Instance);

        var results = await sut.ScanAsync("/root", ct).ToListAsync(ct);

        results.ShouldBeEmpty();
    }
}
