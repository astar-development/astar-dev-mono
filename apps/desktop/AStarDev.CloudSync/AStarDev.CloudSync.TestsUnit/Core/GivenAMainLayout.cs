using AStarDev.CloudSync.Core;

namespace AStarDev.CloudSync.TestsUnit.Core;

public class GivenAMainLayout
{
    [Theory]
    [InlineData("A", typeof(MainLayout.UnifiedAccounts))]
    [InlineData("B", typeof(MainLayout.GroupedByProvider))]
    [InlineData("C", typeof(MainLayout.SyncPairsFirst))]
    [InlineData(" b ", typeof(MainLayout.GroupedByProvider))]
    [InlineData("c", typeof(MainLayout.SyncPairsFirst))]
    public void when_created_from_a_known_key_then_the_matching_layout_is_returned(string key, Type expected)
    {
        var layout = MainLayoutFactory.FromKey(key);

        layout.ShouldBeOfType(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Z")]
    public void when_created_from_an_unknown_or_missing_key_then_the_default_layout_a_is_returned(string? key)
    {
        var layout = MainLayoutFactory.FromKey(key);

        layout.ShouldBeOfType<MainLayout.UnifiedAccounts>();
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public void when_a_layout_is_converted_to_its_key_then_the_key_reads_back_as_the_same_layout(string key)
    {
        var layout = MainLayoutFactory.FromKey(key);

        layout.ToKey().ShouldBe(key);
    }
}
