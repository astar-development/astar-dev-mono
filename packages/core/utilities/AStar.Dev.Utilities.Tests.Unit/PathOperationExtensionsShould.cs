namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class PathOperationExtensionsShould
{
    [Fact]
    public void CombinePath_ReturnsCombinedPathForRelativeSegments()
    {
        var basePath = Path.Join("root", "base");

        var result = basePath.CombinePath("child", "file.txt");

        result.ShouldBe("root/base/child/file.txt");
    }

    [Fact]
    public void CombinePath_AllowsChainingWithoutRootedSegments()
    {
        var result = "base".CombinePath("child2").CombinePath("file.txt");

        result.ShouldBe("base/child2/file.txt");
    }

    [Fact]
    public void CombinePath_ThrowsWhenSegmentIsRooted()
    {
        var root = Path.GetPathRoot(Environment.CurrentDirectory) ?? Path.DirectorySeparatorChar.ToString();
        var rootedSegment = Path.Combine(root, "rooted");

        _ = Should.Throw<ArgumentException>(() => "base".CombinePath(rootedSegment));
    }
}
