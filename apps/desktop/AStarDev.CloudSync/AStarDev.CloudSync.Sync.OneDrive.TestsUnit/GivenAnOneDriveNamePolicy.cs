using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.OneDrive.TestsUnit;

public sealed class GivenAnOneDriveNamePolicy : IDisposable
{
    private const long MaxFileBytes = 250L * 1024 * 1024 * 1024;

    private readonly ServiceProvider provider = new ServiceCollection().AddOneDriveSync().BuildServiceProvider();

    private IRemoteNamePolicy Policy => provider.GetRequiredKeyedService<IRemoteNamePolicy>(CloudProviderFactory.CreateOneDrive());

    private static RelativeSyncPath At(string value) => RelativeSyncPathFactory.Create(value).Match(path => path, _ => throw new InvalidOperationException("The test path must be valid."));

    private object Outcome(string name, long size = 1, string? path = null) => Policy.Check(name, At(path ?? name), size).Match(_ => (object)"accepted", reason => reason);

    [Theory]
    [InlineData("a\"b")]
    [InlineData("a*b")]
    [InlineData("a:b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a?b")]
    [InlineData("a|b")]
    [InlineData("a\\b")]
    public void when_a_name_has_a_forbidden_character_then_it_is_rejected_as_an_illegal_character(string name)
    {
        Outcome(name, path: "Docs/x").ShouldBe(InvalidRemoteNameFactory.CreateIllegalCharacter());
    }

    [Theory]
    [InlineData(" a.txt")]
    [InlineData("a.txt ")]
    public void when_a_name_starts_or_ends_with_a_space_then_it_is_rejected(string name)
    {
        Outcome(name, path: "Docs/x").ShouldBe(InvalidRemoteNameFactory.CreateLeadingOrTrailingSpace());
    }

    [Fact]
    public void when_a_name_ends_with_a_dot_then_it_is_rejected()
    {
        Outcome("a.", path: "Docs/x").ShouldBe(InvalidRemoteNameFactory.CreateEndsWithDot());
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM0")]
    [InlineData("com9")]
    [InlineData("LPT0")]
    [InlineData("lpt9")]
    [InlineData(".lock")]
    [InlineData("desktop.ini")]
    [InlineData("Desktop.INI")]
    [InlineData("~$report.docx")]
    [InlineData("my_vti_file")]
    [InlineData("a_VTI_b")]
    public void when_a_name_is_reserved_then_it_is_rejected(string name)
    {
        Outcome(name, path: "Docs/x").ShouldBe(InvalidRemoteNameFactory.CreateReservedName());
    }

    [Theory]
    [InlineData("COM10")]
    [InlineData("LPT")]
    [InlineData("console")]
    [InlineData("a~$b")]
    public void when_a_name_only_resembles_a_reserved_name_then_it_is_accepted(string name)
    {
        Outcome(name, path: "Docs/x").ShouldBe("accepted");
    }

    [Fact]
    public void when_a_path_is_longer_than_400_characters_then_it_is_rejected_as_too_long()
    {
        Outcome("a.txt", path: new string('a', 401)).ShouldBe(InvalidRemoteNameFactory.CreatePathTooLong());
    }

    [Fact]
    public void when_a_path_is_exactly_400_characters_then_it_is_accepted()
    {
        Outcome("a.txt", path: new string('a', 400)).ShouldBe("accepted");
    }

    [Fact]
    public void when_a_file_is_over_250_gigabytes_then_it_is_rejected_as_too_large()
    {
        Outcome("a.txt", MaxFileBytes + 1, "Docs/a.txt").ShouldBe(InvalidRemoteNameFactory.CreateFileTooLarge());
    }

    [Fact]
    public void when_a_file_is_exactly_250_gigabytes_then_it_is_accepted()
    {
        Outcome("a.txt", MaxFileBytes, "Docs/a.txt").ShouldBe("accepted");
    }

    [Theory]
    [InlineData("report 2026.docx")]
    [InlineData("Ünïcödé-名前.txt")]
    [InlineData(".gitignore")]
    public void when_a_name_is_ordinary_then_it_is_accepted(string name)
    {
        Outcome(name, path: "Docs/x").ShouldBe("accepted");
    }

    [Fact]
    public void when_the_module_is_added_then_the_name_policy_is_registered_keyed_by_onedrive()
    {
        var services = new ServiceCollection().AddOneDriveSync();

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IRemoteNamePolicy) && descriptor.IsKeyedService && Equals(descriptor.ServiceKey, CloudProviderFactory.CreateOneDrive()) && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    public void Dispose() => provider.Dispose();
}
