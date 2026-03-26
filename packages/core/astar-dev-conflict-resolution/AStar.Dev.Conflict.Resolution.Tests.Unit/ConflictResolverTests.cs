namespace AStar.Dev.Conflict.Resolution.Tests.Unit;

public class ConflictResolverTests
{
    private readonly IConflictStore _store = Substitute.For<IConflictStore>();

    private ConflictResolver CreateResolver()
    {
        _store.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConflictRecord>>([]));
        return new ConflictResolver(_store);
    }

    [Fact]
    public async Task AddConflictAsync_PersistsRecord()
    {
        var ct = TestContext.Current.CancellationToken;
        var resolver = CreateResolver();
        await resolver.InitialiseAsync(ct);

        var record = new ConflictRecord { FileName = "file.txt", FilePath = "docs/file.txt" };
        await resolver.AddConflictAsync(record, ct);

        await _store.Received(1).SaveAsync(
            Arg.Is<IReadOnlyList<ConflictRecord>>(r => r.Count == 1 && r[0].FileName == "file.txt"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyUnresolved()
    {
        var ct = TestContext.Current.CancellationToken;
        var resolver = CreateResolver();
        await resolver.InitialiseAsync(ct);

        var resolved = new ConflictRecord
        {
            FileName = "resolved.txt",
            FilePath = "docs/resolved.txt",
            ResolvedPolicy = ConflictPolicy.LocalWins,
            ResolvedAtUtc = DateTimeOffset.UtcNow
        };
        var pending = new ConflictRecord { FileName = "pending.txt", FilePath = "docs/pending.txt" };

        await resolver.AddConflictAsync(resolved, ct);
        await resolver.AddConflictAsync(pending, ct);

        var result = await resolver.GetPendingAsync(ct);

        result.Count.ShouldBe(1);
        result[0].FileName.ShouldBe("pending.txt");
    }

    [Fact]
    public async Task ResolveAsync_ResolvesSelectedConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var resolver = CreateResolver();
        await resolver.InitialiseAsync(ct);

        var record = new ConflictRecord { FileName = "file.txt", FilePath = "docs/file.txt" };
        await resolver.AddConflictAsync(record, ct);

        var resolvedIds = await resolver.ResolveAsync([record.Id], ConflictPolicy.RemoteWins, ct);

        resolvedIds.ShouldContain(record.Id);
        record.ResolvedPolicy.ShouldBe(ConflictPolicy.RemoteWins);
        record.ResolvedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task ResolveAsync_CascadesToMatchingFilePath()
    {
        var ct = TestContext.Current.CancellationToken;
        var resolver = CreateResolver();
        await resolver.InitialiseAsync(ct);

        var first = new ConflictRecord { FileName = "file.txt", FilePath = "docs/file.txt" };
        var second = new ConflictRecord { FileName = "file.txt", FilePath = "docs/file.txt" };
        var unrelated = new ConflictRecord { FileName = "other.txt", FilePath = "docs/other.txt" };

        await resolver.AddConflictAsync(first, ct);
        await resolver.AddConflictAsync(second, ct);
        await resolver.AddConflictAsync(unrelated, ct);

        var resolvedIds = await resolver.ResolveAsync([first.Id], ConflictPolicy.KeepBoth, ct);

        resolvedIds.Count.ShouldBe(2);
        resolvedIds.ShouldContain(first.Id);
        resolvedIds.ShouldContain(second.Id);
        unrelated.IsResolved.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveAsync_SkipsAlreadyResolved()
    {
        var ct = TestContext.Current.CancellationToken;
        var resolver = CreateResolver();
        await resolver.InitialiseAsync(ct);

        var record = new ConflictRecord
        {
            FileName = "file.txt",
            FilePath = "docs/file.txt",
            ResolvedPolicy = ConflictPolicy.LocalWins,
            ResolvedAtUtc = DateTimeOffset.UtcNow
        };
        await resolver.AddConflictAsync(record, ct);

        var resolvedIds = await resolver.ResolveAsync([record.Id], ConflictPolicy.RemoteWins, ct);

        resolvedIds.ShouldBeEmpty();
        record.ResolvedPolicy.ShouldBe(ConflictPolicy.LocalWins);
    }

    [Theory]
    [InlineData("report.docx", "report-(2026-03-15T143022Z).docx")]
    [InlineData("photo.jpg", "photo-(2026-03-15T143022Z).jpg")]
    [InlineData("archive.tar.gz", "archive.tar-(2026-03-15T143022Z).gz")]
    [InlineData("noext", "noext-(2026-03-15T143022Z)")]
    public void GenerateKeepBothName_ProducesCorrectFormat(string original, string expected)
    {
        var resolver = CreateResolver();
        var utc = new DateTimeOffset(2026, 3, 15, 14, 30, 22, TimeSpan.Zero);

        var result = resolver.GenerateKeepBothName(original, utc);

        result.ShouldBe(expected);
    }
}
