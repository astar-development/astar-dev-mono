namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class PathOperationExtensionsShould
{
    [Fact]
    public void CombinePath_ReturnsCombinedPathForRelativeSegments()
    {
        string basePath = Path.Join("root", "base");

        string result = basePath.CombinePath("child", "file.txt");

        result.ShouldBe("root/base/child/file.txt");
    }

    [Fact]
    public void CombinePath_AllowsChainingWithoutRootedSegments()
    {
        string result = "base".CombinePath("child2").CombinePath("file.txt");

        result.ShouldBe("base/child2/file.txt");
    }

    [Fact]
    public void CombinePath_ThrowsWhenSegmentIsRooted()
    {
        string root = Path.GetPathRoot(Environment.CurrentDirectory) ?? Path.DirectorySeparatorChar.ToString();
        string rootedSegment = Path.Combine(root, "rooted");

        _ = Should.Throw<ArgumentException>(() => "base".CombinePath(rootedSegment));
    }
}
