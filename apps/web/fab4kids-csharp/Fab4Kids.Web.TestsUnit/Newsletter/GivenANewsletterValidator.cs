using AStar.Dev.FunctionalParadigm;
using Fab4Kids.Web.Newsletter;

namespace Fab4Kids.Web.TestsUnit.Newsletter;

public class GivenANewsletterValidator
{
    [Fact]
    public void when_the_email_is_valid_then_the_trimmed_email_is_returned()
    {
        var sut = NewsletterValidator.Validate("  ada@example.com  ");

        sut.TryGetValue(out string email).ShouldBeTrue();
        email.ShouldBe("ada@example.com");
    }

    [Fact]
    public void when_the_email_is_empty_then_validation_fails()
    {
        var sut = NewsletterValidator.Validate("   ");

        sut.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "email" && e.Message == "Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("no-at-sign.com")]
    public void when_the_email_is_malformed_then_validation_fails(string email)
    {
        var sut = NewsletterValidator.Validate(email);

        sut.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "email");
    }
}
