using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Domain;

public sealed class GivenARuleBasedFileAutoCategorisorWithCodeFiles
{
    [Fact]
    public void when_a_power_shell_module_script_is_categorised_then_no_classification_is_produced()
    {
        var sut = new RuleBasedFileAutoCategorisor();

        var result = sut.Categorise("/a/b/c/d/e/f/g/Add-AzADAppPermission.ps1");

        result.Match(_ => false, () => true).ShouldBeTrue();
    }
}
