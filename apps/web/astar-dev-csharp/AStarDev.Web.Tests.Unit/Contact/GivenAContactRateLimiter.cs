using AStarDev.Web.Contact;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.Web.Tests.Unit.Contact;

public class GivenAContactRateLimiter
{
    private readonly FakeTimeProvider timeProvider = new();

    private ContactRateLimiter CreateSut() => new(timeProvider);

    [Fact]
    public void when_a_new_ip_address_makes_its_first_request_then_it_is_allowed()
    {
        var sut = CreateSut();

        sut.TryAcquire("1.2.3.4").ShouldBeTrue();
    }

    [Fact]
    public void when_an_ip_address_makes_ten_requests_within_the_window_then_all_are_allowed()
    {
        var sut = CreateSut();

        for (int i = 0; i < 10; i++)
            sut.TryAcquire("1.2.3.4").ShouldBeTrue();
    }

    [Fact]
    public void when_an_ip_address_makes_an_eleventh_request_within_the_window_then_it_is_blocked()
    {
        var sut = CreateSut();
        for (int i = 0; i < 10; i++)
            sut.TryAcquire("1.2.3.4");

        sut.TryAcquire("1.2.3.4").ShouldBeFalse();
    }

    [Fact]
    public void when_different_ip_addresses_make_requests_then_each_has_its_own_budget()
    {
        var sut = CreateSut();
        for (int i = 0; i < 10; i++)
            sut.TryAcquire("1.2.3.4");

        sut.TryAcquire("5.6.7.8").ShouldBeTrue();
    }

    [Fact]
    public void when_the_window_has_elapsed_then_the_budget_resets()
    {
        var sut = CreateSut();
        for (int i = 0; i < 10; i++)
            sut.TryAcquire("1.2.3.4");
        sut.TryAcquire("1.2.3.4").ShouldBeFalse();

        timeProvider.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        sut.TryAcquire("1.2.3.4").ShouldBeTrue();
    }
}
