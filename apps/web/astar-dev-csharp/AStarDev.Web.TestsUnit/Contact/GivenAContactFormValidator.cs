using AStar.Dev.FunctionalParadigm;
using AStarDev.Web.Contact;

namespace AStarDev.Web.TestsUnit.Contact;

public class GivenAContactFormValidator
{
    [Fact]
    public void when_all_fields_are_valid_then_a_contact_message_is_returned()
    {
        var validation = ContactFormValidator.Validate("Ada Lovelace", "ada@example.com", "Interested in working together.", sendCopy: true);

        validation.TryGetValue(out var message).ShouldBeTrue();
        message.Name.ShouldBe("Ada Lovelace");
        message.Email.ShouldBe("ada@example.com");
        message.Message.ShouldBe("Interested in working together.");
        message.SendCopy.ShouldBeTrue();
    }

    [Fact]
    public void when_fields_have_surrounding_whitespace_then_the_trimmed_values_are_used()
    {
        var validation = ContactFormValidator.Validate("  Ada Lovelace  ", " ada@example.com ", "  Interested in working together.  ", sendCopy: false);

        validation.TryGetValue(out var message).ShouldBeTrue();
        message.Name.ShouldBe("Ada Lovelace");
        message.Email.ShouldBe("ada@example.com");
        message.Message.ShouldBe("Interested in working together.");
    }

    [Fact]
    public void when_the_name_is_empty_then_a_name_error_is_returned()
    {
        var validation = ContactFormValidator.Validate("", "ada@example.com", "Interested in working together.", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "name" && e.Message == "Name is required.");
    }

    [Fact]
    public void when_the_name_is_too_long_then_a_name_error_is_returned()
    {
        var validation = ContactFormValidator.Validate(new string('a', 201), "ada@example.com", "Interested in working together.", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "name" && e.Message == "Name must be 200 characters or fewer.");
    }

    [Fact]
    public void when_the_email_is_empty_then_an_email_error_is_returned()
    {
        var validation = ContactFormValidator.Validate("Ada Lovelace", "", "Interested in working together.", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "email" && e.Message == "Email is required.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.com")]
    [InlineData("spaces in@email.com")]
    public void when_the_email_is_not_valid_then_an_email_error_is_returned(string email)
    {
        var validation = ContactFormValidator.Validate("Ada Lovelace", email, "Interested in working together.", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "email" && e.Message == "Please enter a valid email address.");
    }

    [Fact]
    public void when_the_message_is_empty_then_a_message_error_is_returned()
    {
        var validation = ContactFormValidator.Validate("Ada Lovelace", "ada@example.com", "", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "message" && e.Message == "Message is required.");
    }

    [Fact]
    public void when_the_message_is_too_short_then_a_message_error_is_returned()
    {
        var validation = ContactFormValidator.Validate("Ada Lovelace", "ada@example.com", "Too short", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "message" && e.Message == "Message must be at least 10 characters.");
    }

    [Fact]
    public void when_the_message_is_too_long_then_a_message_error_is_returned()
    {
        var validation = ContactFormValidator.Validate("Ada Lovelace", "ada@example.com", new string('a', 5001), sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.ShouldContain(e => e.Property == "message" && e.Message == "Message must be 5000 characters or fewer.");
    }

    [Fact]
    public void when_multiple_fields_are_invalid_then_all_errors_are_accumulated()
    {
        var validation = ContactFormValidator.Validate("", "", "", sendCopy: false);

        validation.TryGetErrors(out var errors).ShouldBeTrue();
        errors.Select(e => e.Property).ShouldBe(["name", "email", "message"]);
    }
}
