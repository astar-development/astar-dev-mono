using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenASha256ContentHasher
{
    private const string EmptyFileHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private const string AbcHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    private const string LargeFileHash = "4a3f0c0c213adea174f9a3d4c13177315b588bdb2e9c1012d3d0bf0453ca0f6a";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_file_is_empty_then_the_hash_is_the_known_empty_vector()
    {
        var hash = await Hash(Write("/root/empty.bin", []));

        hash.ShouldBe(EmptyFileHash);
    }

    [Fact]
    public async Task when_the_file_holds_abc_then_the_hash_is_the_known_vector()
    {
        var hash = await Hash(Write("/root/abc.txt", "abc"u8.ToArray()));

        hash.ShouldBe(AbcHash);
    }

    [Fact]
    public async Task when_the_file_is_larger_than_one_megabyte_then_the_hash_is_the_known_vector()
    {
        var hash = await Hash(Write("/root/large.bin", Enumerable.Repeat((byte)'a', (1024 * 1024) + 1).ToArray()));

        hash.ShouldBe(LargeFileHash);
    }

    [Fact]
    public async Task when_the_file_does_not_exist_then_the_hash_fails_as_unreadable()
    {
        var hash = await Hash(fileSystem.FileInfo.New("/root/vanished.txt"));

        hash.ShouldBe("Unreadable");
    }

    [Fact]
    public async Task when_the_file_is_locked_then_the_hash_fails_as_unreadable()
    {
        var file = Write("/root/locked.txt", "abc"u8.ToArray());
        await using var holder = fileSystem.File.Open("/root/locked.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var hash = await Hash(file);

        hash.ShouldBe("Unreadable");
    }

    [Fact]
    public async Task when_the_token_is_already_cancelled_then_the_hash_is_cancelled_and_not_a_failure()
    {
        var file = Write("/root/abc.txt", "abc"u8.ToArray());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var outcome = await Should.ThrowAsync<OperationCanceledException>(() => new Sha256ContentHasher().HashAsync(file, source.Token));

        outcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task when_many_files_are_hashed_at_once_then_each_gets_its_own_hash()
    {
        var files = Enumerable.Range(0, 20).Select(index => Write($"/root/file{index}.txt", "abc"u8.ToArray())).ToList();

        var hashes = await Task.WhenAll(files.Select(file => Task.Run(() => Hash(file), CancellationToken)));

        hashes.ShouldAllBe(hash => hash == AbcHash);
    }

    private static async Task<string> Hash(IFileInfo file)
    {
        var result = await new Sha256ContentHasher().HashAsync(file, CancellationToken);

        return result.Match(hash => hash.Value, failure => failure is LocalScanFailure.Unreadable ? "Unreadable" : "Unexpected");
    }

    private IFileInfo Write(string path, byte[] content)
    {
        _ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        fileSystem.File.WriteAllBytes(path, content);

        return fileSystem.FileInfo.New(path);
    }
}
