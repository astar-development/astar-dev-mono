using Testably.Abstractions;

namespace AStarDev.SingleInstance.TestsUnit;

public class GivenSingleInstanceOptionsFactory
{
    private static readonly System.IO.Abstractions.IDirectoryInfo Folder = new RealFileSystem().DirectoryInfo.New("/tmp/jane.doe@example.com");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_name_is_blank_then_the_default_name_is_used(string? name)
    {
        var options = SingleInstanceOptionsFactory.Create(Folder, name, null);

        options.Name.ShouldBe(SingleInstanceOptionsFactory.DefaultName);
    }

    [Fact]
    public void when_a_name_is_given_then_it_is_trimmed()
    {
        var options = SingleInstanceOptionsFactory.Create(Folder, "  cloudsync ", null);

        options.Name.ShouldBe("cloudsync");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void when_the_timeout_is_missing_or_not_positive_then_the_default_timeout_is_used(int? milliseconds)
    {
        var options = SingleInstanceOptionsFactory.Create(Folder, "app", milliseconds is { } value ? TimeSpan.FromMilliseconds(value) : null);

        options.HandOffTimeout.ShouldBe(SingleInstanceOptionsFactory.DefaultHandOffTimeout);
    }

    [Fact]
    public void when_a_positive_timeout_is_given_then_it_is_kept()
    {
        var options = SingleInstanceOptionsFactory.Create(Folder, "app", TimeSpan.FromMilliseconds(750));

        options.HandOffTimeout.ShouldBe(TimeSpan.FromMilliseconds(750));
    }

    [Fact]
    public void when_the_options_are_printed_then_the_folder_is_redacted()
    {
        var options = SingleInstanceOptionsFactory.Create(Folder, "app", null);

        options.ToString().ShouldNotContain("jane.doe");
    }
}
