namespace AStar.Dev.Velopack.Publishing.TestsUnit;

public sealed class GivenAnUpdateDeferralAfterDeferring
{
    private static readonly DateTimeOffset Earlier = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = Earlier.AddHours(3);

    [Fact]
    public void when_nothing_was_deferred_before_then_both_times_are_now()
    {
        var deferral = UpdateDeferralFactory.CreateAfterDeferring(UpdateDeferralFactory.CreateNotDeferred(), "1.2.0", Now);

        var deferred = deferral.ShouldBeOfType<DeferredSince>();
        deferred.Version.ShouldBe("1.2.0");
        deferred.FirstDeferredAt.ShouldBe(Now);
        deferred.LastPromptedAt.ShouldBe(Now);
    }

    [Fact]
    public void when_the_same_version_was_deferred_before_then_the_first_time_is_kept()
    {
        var before = UpdateDeferralFactory.CreateDeferredSince("1.2.0", Earlier, Earlier);

        var deferred = UpdateDeferralFactory.CreateAfterDeferring(before, "1.2.0", Now).ShouldBeOfType<DeferredSince>();

        deferred.FirstDeferredAt.ShouldBe(Earlier);
        deferred.LastPromptedAt.ShouldBe(Now);
    }

    [Fact]
    public void when_an_older_version_was_deferred_before_then_the_count_starts_again()
    {
        var before = UpdateDeferralFactory.CreateDeferredSince("1.1.0", Earlier, Earlier);

        var deferred = UpdateDeferralFactory.CreateAfterDeferring(before, "1.2.0", Now).ShouldBeOfType<DeferredSince>();

        deferred.Version.ShouldBe("1.2.0");
        deferred.FirstDeferredAt.ShouldBe(Now);
    }
}
