using Fab4Kids.Web.Newsletter;

namespace Fab4Kids.Web.TestsUnit.Newsletter;

public class GivenANewsletterSubscriberFactory
{
    [Fact]
    public void when_created_with_valid_values_then_they_are_preserved()
    {
        var subscribedAt = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

        var sut = NewsletterSubscriberFactory.Create("Ada@Example.com", subscribedAt);

        sut.Email.ShouldBe("ada@example.com");
        sut.SubscribedAt.ShouldBe(subscribedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void when_created_with_a_blank_email_then_it_is_normalized_to_empty(string? email)
    {
        var sut = NewsletterSubscriberFactory.Create(email, DateTimeOffset.UtcNow);

        sut.Email.ShouldBe(string.Empty);
    }

    [Fact]
    public void when_created_with_an_untrimmed_email_then_it_is_trimmed()
    {
        var sut = NewsletterSubscriberFactory.Create("  ada@example.com  ", DateTimeOffset.UtcNow);

        sut.Email.ShouldBe("ada@example.com");
    }
}
