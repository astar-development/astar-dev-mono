using AStarDev.CloudSync.Accounts;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAStaggerPolicy
{
    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);
    private static readonly IStaggerPolicy Policy = new ServiceCollection().AddCloudSync().BuildServiceProvider().GetRequiredService<IStaggerPolicy>();

    private static IReadOnlyList<AccountId> Accounts(int count) => [.. Enumerable.Range(1, count).Select(number => AccountIdFactory.Create(new Guid(number, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0])))];

    [Theory]
    [InlineData(1, new[] { 0 })]
    [InlineData(2, new[] { 0, 30 })]
    [InlineData(3, new[] { 0, 20, 40 })]
    [InlineData(4, new[] { 0, 15, 30, 45 })]
    public void when_accounts_share_an_hour_then_their_offsets_are_even_and_in_the_order_given(int count, int[] expectedMinutes)
    {
        var accounts = Accounts(count);

        var offsets = Policy.OffsetsFor(accounts, Hour);

        accounts.Select(account => offsets[account]).ShouldBe(expectedMinutes.Select(minutes => TimeSpan.FromMinutes((double)minutes)));
    }

    [Fact]
    public void when_there_are_no_accounts_then_there_are_no_offsets()
    {
        Policy.OffsetsFor([], Hour).ShouldBeEmpty();
    }

    [Fact]
    public void when_the_interval_is_four_hours_then_four_accounts_start_an_hour_apart()
    {
        var accounts = Accounts(4);

        var offsets = Policy.OffsetsFor(accounts, TimeSpan.FromHours(4));

        accounts.Select(account => offsets[account]).ShouldBe([TimeSpan.Zero, Hour, TimeSpan.FromHours(2), TimeSpan.FromHours(3)]);
    }

    [Fact]
    public void when_the_same_account_is_listed_twice_then_it_has_one_offset_and_no_error()
    {
        var account = Accounts(1)[0];

        var offsets = Policy.OffsetsFor([account, account], Hour);

        offsets.Count.ShouldBe(1);
    }
}
