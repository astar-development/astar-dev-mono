namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenATransferConcurrencyFactory
{
    [Theory]
    [InlineData(int.MinValue, 1)]
    [InlineData(-4, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(8, 8)]
    [InlineData(9, 8)]
    [InlineData(int.MaxValue, 8)]
    public void when_a_value_is_created_then_it_is_clamped_to_one_to_eight(int requested, int expected)
    {
        TransferConcurrencyFactory.Create(requested).Value.ShouldBe(expected);
    }

    [Fact]
    public void when_the_default_is_created_then_it_is_four()
    {
        TransferConcurrencyFactory.CreateDefault().Value.ShouldBe(4);
    }
}
