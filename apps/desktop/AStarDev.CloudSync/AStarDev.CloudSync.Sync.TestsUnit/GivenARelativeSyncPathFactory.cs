using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenARelativeSyncPathFactory
{
    [Theory]
    [InlineData("Documents/Report.docx", "Documents/Report.docx")]
    [InlineData("Documents\\Reports\\Q1.xlsx", "Documents/Reports/Q1.xlsx")]
    [InlineData("/Documents/Photos/", "Documents/Photos")]
    [InlineData("  Documents/Notes.txt  ", "Documents/Notes.txt")]
    public void when_a_path_is_created_then_it_is_normalised(string value, string expected)
    {
        var created = RelativeSyncPathFactory.Create(value);

        created.Match(path => path.Value, error => error.Message).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    [InlineData("Documents/../Secrets")]
    [InlineData("./Documents")]
    [InlineData("Documents//Report.docx")]
    public void when_a_path_is_empty_or_escapes_the_root_then_it_is_rejected(string? value)
    {
        var created = RelativeSyncPathFactory.Create(value);

        created.Match(_ => "created", _ => "rejected").ShouldBe("rejected");
    }

    [Fact]
    public void when_a_path_is_printed_then_it_is_redacted()
    {
        var path = SyncPaths.Of("Documents/jane.doe@example.com.txt");

        path.ToString().ShouldNotContain("jane.doe");
    }

    [Fact]
    public void when_two_paths_differ_only_in_case_then_the_comparer_treats_them_as_equal()
    {
        var comparer = RelativeSyncPathComparer.Instance;

        (comparer.Equals(SyncPaths.Of("Documents/Report.docx"), SyncPaths.Of("documents/REPORT.docx")), comparer.GetHashCode(SyncPaths.Of("Documents/Report.docx")) == comparer.GetHashCode(SyncPaths.Of("documents/REPORT.docx"))).ShouldBe((true, true));
    }
}
